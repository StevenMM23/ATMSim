using System.Security.Cryptography;

namespace ATMSim;


// Extract Class y Extract Method 

#region Extract Class

public class LlaveMaestra
{
    private readonly Aes llaveMaestra;

    public LlaveMaestra()
    {
        llaveMaestra = Aes.Create();
    }

    public byte[] EncriptarLlave(byte[] llaveIv)
    {
        var encriptador = CrearTransformador(true);

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encriptador, CryptoStreamMode.Write);
        cs.Write(llaveIv);
        var llaveEncriptada = ms.ToArray();
        return llaveEncriptada;
    }

    public byte[] DesencriptarLlave(byte[] criptogramaLlaveIv)
    {
        var desencriptador = CrearTransformador(false);

        using var ms = new MemoryStream(criptogramaLlaveIv);
        using var cs = new CryptoStream(ms, desencriptador, CryptoStreamMode.Read);
        var desencriptado = new byte[criptogramaLlaveIv.Length];
        var totalBytesLeidos = 0;
        while (totalBytesLeidos < criptogramaLlaveIv.Length)
        {
            var bytesLeidos = cs.Read(desencriptado, totalBytesLeidos,
                criptogramaLlaveIv.Length - totalBytesLeidos);
            if (bytesLeidos == 0)
                break;
            totalBytesLeidos += bytesLeidos;
        }

        return desencriptado;
    }


    #region Extract Method

    private ICryptoTransform CrearTransformador(bool esCifrado)
    {
        var llave = llaveMaestra.Key;
        var iv = llaveMaestra.IV;
        var aes = Aes.Create();
        aes.Key = llave;
        aes.IV = iv;
        aes.Padding = PaddingMode.None;
        return esCifrado ? aes.CreateEncryptor() : aes.CreateDecryptor();
    }
    #endregion
}

#endregion

