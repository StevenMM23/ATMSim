using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace ATMSim
{
    public struct ComponentesLlave
    {
        public byte[] LlaveEnClaro { get; private set; }
        public byte[] LlaveEncriptada { get; private set; }
        public ComponentesLlave(byte[] llaveEnClaro, byte[] llaveEncriptada)
            => (LlaveEnClaro, LlaveEncriptada) = (llaveEnClaro, llaveEncriptada);
    }

    public interface IHSM
    {
        public ComponentesLlave GenerarLlave();
        public byte[] TraducirPin(byte[] criptograma, byte[] criptogramaLlaveOrigen, byte[] criptogramaLlaveDestino);

        public byte[] EncriptarPinConLlaveMaestra(string pin);

        public bool ValidarPin(byte[] criptogramaPinAValidar, byte[] criptogramaLlave, byte[] criptogramaPinCorrecto);

    }
    public class HSM : IHSM
    {
        private const int TAMANO_LLAVE = 32; // bytes

        private Aes lmk; // llave maestra (Local Master Key)
        public HSM()
        {
            this.lmk = Aes.Create();
        }

        public ComponentesLlave GenerarLlave()
        {
            Aes llaveAes = Aes.Create();
            byte[] llaveIv = CombinarLlaveConIV(llaveAes.Key, llaveAes.IV);
            byte[] criptogramaLlaveIv = EncriptarLlave(llaveIv);


            return new ComponentesLlave(llaveIv, criptogramaLlaveIv);
        }


        public byte[] TraducirPin(byte[] criptograma, byte[] criptogramaLlaveOrigen, byte[] criptogramaLlaveDestino)
        {
            byte[] llaveOrigenIv = DesencriptarLlave(criptogramaLlaveOrigen);

            byte[] llaveDestinoIv = DesencriptarLlave(criptogramaLlaveDestino);

            string textoPlano = DesencriptarDato(criptograma, llaveOrigenIv);
            byte[] nuevoCriptograma = EncriptarDato(textoPlano, llaveDestinoIv);

            return nuevoCriptograma;
        }

        public byte[] EncriptarPinConLlaveMaestra(string pin)
        {
            return EncriptarDato(pin, this.lmk);
        }

        public bool ValidarPin(byte[] criptogramaPinAValidar, byte[] criptogramaLlaveAutorizador, byte[] criptogramaPinCorrecto)
        {
            byte[] llaveAutorizador = DesencriptarLlave(criptogramaLlaveAutorizador);

            string pinCorrecto = DesencriptarDato(criptogramaPinCorrecto, this.lmk);
            string pinAValidar = DesencriptarDato(criptogramaPinAValidar, llaveAutorizador);

            return pinCorrecto == pinAValidar;
        }

        private byte[] CombinarLlaveConIV(byte[] llave, byte[] iv) => llave.Concat(iv).ToArray();
        private byte[] ExtraerLlave(byte[] llaveIv) => llaveIv.Skip(0).Take(TAMANO_LLAVE).ToArray();
        private byte[] ExtraerIV(byte[] llaveIv) => llaveIv.Skip(TAMANO_LLAVE).ToArray();


        //Extract Method Refactoring - Equipo 1

        #region Extract Method Refactoring

        #region Codigo Nuevo

        private ICryptoTransform CrearTransformador(bool esCifrado)
        {
            var llave = lmk.Key;
            var iv = lmk.IV;
            Aes aes = Aes.Create();
            aes.Key = llave;
            aes.IV = iv;
            aes.Padding = PaddingMode.None;
            return esCifrado ? aes.CreateEncryptor() : aes.CreateDecryptor();
        }

        private byte[] EncriptarLlave(byte[] llaveIv)
        {
            ICryptoTransform encriptador = CrearTransformador(true);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encriptador, CryptoStreamMode.Write))
                {
                    cs.Write(llaveIv);
                    var llaveEncriptada = ms.ToArray();
                    return llaveEncriptada;
                }
            }
        }


        private byte[] DesencriptarLlave(byte[] criptogramaLlaveIv)
        {
            ICryptoTransform desencriptador = CrearTransformador(false);

            using (MemoryStream ms = new MemoryStream(criptogramaLlaveIv))
            {
                using (CryptoStream cs = new CryptoStream(ms, desencriptador, CryptoStreamMode.Read))
                {
                    var desencriptado = new byte[criptogramaLlaveIv.Length];
                    int totalBytesLeidos = 0;
                    while (totalBytesLeidos < criptogramaLlaveIv.Length)
                    {
                        int bytesLeidos = cs.Read(desencriptado, totalBytesLeidos, criptogramaLlaveIv.Length - totalBytesLeidos);
                        if (bytesLeidos == 0)
                        {
                            break;
                        }
                        totalBytesLeidos += bytesLeidos;
                    }
                    Array.Resize(ref desencriptado, totalBytesLeidos);
                    return desencriptado;
                }
            }
        }

        #endregion


        #region Codigo Anterior

        //Codigo Anterior: 

        // private byte[] EncriptarLlave(byte[] llaveIv)
        // {
        //     var llave = lmk.Key;
        //     var iv = lmk.IV;
        //     Aes aes = Aes.Create();
        //     aes.Key = llave;
        //     aes.IV = iv;
        //     aes.Padding = PaddingMode.None;
        //     ICryptoTransform encriptador = aes.CreateEncryptor();
        //
        //     using (MemoryStream ms = new MemoryStream())
        //     {
        //         using (CryptoStream cs = new CryptoStream(ms, encriptador, CryptoStreamMode.Write))
        //         {
        //             cs.Write(llaveIv);
        //             var llaveEncriptada = ms.ToArray();
        //             return llaveEncriptada;
        //         }
        //     }
        // }
        //
        //
        // private byte[] DesencriptarLlave(byte[] criptogramaLlaveIv)
        // {
        //     var llave = lmk.Key;
        //     var iv = lmk.IV;
        //     Aes aes = Aes.Create();
        //     aes.Key = llave;
        //     aes.IV = iv;
        //     aes.Padding = PaddingMode.None;
        //     ICryptoTransform desencriptador = aes.CreateDecryptor();
        //
        //     using (MemoryStream ms = new MemoryStream(criptogramaLlaveIv))
        //     {
        //         using (CryptoStream cs = new CryptoStream(ms, desencriptador, CryptoStreamMode.Read))
        //         {
        //             var desencriptado = new byte[criptogramaLlaveIv.Length];
        //             int totalBytesLeidos = 0;
        //             while (totalBytesLeidos < criptogramaLlaveIv.Length)
        //             {
        //                 int bytesLeidos = cs.Read(desencriptado, 0, criptogramaLlaveIv.Length);
        //                 if (bytesLeidos == 0) break;
        //                 totalBytesLeidos += bytesLeidos;
        //             }
        //
        //             var llaveEnClaro = desencriptado.Take(totalBytesLeidos).ToArray();
        //             return llaveEnClaro;
        //         }
        //     }
        // }

        #endregion


        #endregion


        private byte[] EncriptarDato(string textoPlano, byte[] llaveIv)
        {
            byte[] llave = ExtraerLlave(llaveIv);
            byte[] iv = ExtraerIV(llaveIv);
            return EncriptarDato(textoPlano, llave, iv);
        }

        private byte[] EncriptarDato(string textoPlano, byte[] llave, byte[] iv)
        {
            using (Aes llaveAes = Aes.Create())
            {
                llaveAes.Key = llave;
                llaveAes.IV = iv;
                return EncriptarDato(textoPlano, llaveAes);
            }
        }

        private byte[] EncriptarDato(string textoPlano, Aes llave)
        {


            ICryptoTransform encriptador = llave.CreateEncryptor();

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encriptador, CryptoStreamMode.Write))
                {
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(textoPlano);
                    }
                    return ms.ToArray();
                }
            }


        }

        private string DesencriptarDato(byte[] criptograma, byte[] llaveIv)
        {
            byte[] llave = ExtraerLlave(llaveIv);
            byte[] iv = ExtraerIV(llaveIv);
            return DesencriptarDato(criptograma, llave, iv);
        }

        private string DesencriptarDato(byte[] criptograma, byte[] llave, byte[] iv)
        {
            using (Aes llaveAes = Aes.Create())
            {
                llaveAes.Key = llave;
                llaveAes.IV = iv;
                return DesencriptarDato(criptograma, llaveAes);
            }
        }

        private string DesencriptarDato(byte[] criptograma, Aes llave)
        {
            ICryptoTransform desencriptador = llave.CreateDecryptor();

            using (MemoryStream ms = new MemoryStream(criptograma))
            {
                using (CryptoStream cs = new CryptoStream(ms, desencriptador, CryptoStreamMode.Read))
                {
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }

        }

    }
}
