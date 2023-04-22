using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace ATMSim;

public interface IATM
{
    public IATMSwitch? Switch { get; set; }
    public string Nombre { get; set; }

    public bool Configurado { get; }

    public void EnviarTransactionRequest(string opKeyBuffer, string numeroTarjeta, string pin, double monto = 0);
    public void InstalarLlave(byte[] llave);
    public void Reestablecer();
}

//Factory Method para desacoplar la creacion de objetos ATM de su implementacion

#region Factory Method

public interface IATMFactory
{
    public IATM CrearATM(string nombre);
}
public class ATMFactory : IATMFactory
{
    private readonly IConsoleWriter consoleWriter;
    private readonly IThreadSleeper threadSleeper;

    public ATMFactory(IConsoleWriter consoleWriter, IThreadSleeper threadSleeper)
    {
        this.consoleWriter = consoleWriter;
        this.threadSleeper = threadSleeper;
    }

    public IATM CrearATM(string nombre)
    {
        return new ATM(nombre, consoleWriter, threadSleeper);
    }
}

#endregion

public class ATMNoEstaRegistradoException : Exception
{
}

public class Comando
{
}

public class ComandoDispensarEfectivo : Comando
{
    public double Monto { get; }

    public ComandoDispensarEfectivo(double monto)
    {
        Monto = monto;
    }
}

public class ComandoImprimirRecibo : Comando
{
    public string TextoRecibo { get; }

    public ComandoImprimirRecibo(string textoRecibo)
    {
        TextoRecibo = textoRecibo;
    }
}

public class ComandoMostrarInfoEnPantalla : Comando
{
    public string TextoPantalla { get; }
    public bool Error { get; }

    public ComandoMostrarInfoEnPantalla(string textoPantalla, bool error = false)
    {
        (TextoPantalla, Error) = (textoPantalla, error);
    }
}

public class ComandoDevolverTarjeta : Comando
{
}
public class Transaccion
{
    public List<Comando> Comandos { get; } = new List<Comando>();

    public void AgregarComando(Comando comando)
    {
        Comandos.Add(comando);
    }
}

public class ATM : IATM
{
    private const int TAMANO_LLAVE = 32; // bytes


    private byte[]? tpk;
    public IATMSwitch? Switch { get; set; }
    public string Nombre { get; set; }

    private readonly IConsoleWriter consoleWriter;

    private readonly IThreadSleeper threadSleeper;

    public ATM(string nombre, IConsoleWriter consoleWriter, IThreadSleeper threadSleeper)
    {
        Nombre = nombre;
        this.consoleWriter = consoleWriter;
        this.threadSleeper = threadSleeper;
    }

    public bool Configurado => tpk != null && Switch != null;

    public void EnviarTransactionRequest(string opKeyBuffer, string numeroTarjeta, string pin, double monto = 0)
    {
        if (!Configurado)
            throw new InvalidOperationException("El ATM aún no está configurado correctamente");

        if (!Regex.Match(pin, @"[0-9]{4}").Success)
            MostrarError("ERROR.\n\nEl Pin debe ser un número de 4 digitos.");


        var criptogramaPin = Encriptar(pin);

        var comandosDeRespuesta = Switch.Autorizar(this, opKeyBuffer, numeroTarjeta, monto, criptogramaPin);
        EjecutarListaComandos(comandosDeRespuesta);
    }


    public void InstalarLlave(byte[] llave)
    {
        tpk = llave;
    }

    public void Reestablecer()
    {
        tpk = null;
        Switch = null;
    }


    private void MostrarError(string mensajeDeError)
    {
        var comandos = new List<Comando>();
        comandos.Add(new ComandoMostrarInfoEnPantalla(mensajeDeError, true));
        EjecutarListaComandos(comandos);
    }

    private void EjecutarListaComandos(List<Comando> comandos)
    {
        foreach (var comando in comandos)
            switch (comando)
            {
                case ComandoDispensarEfectivo cmd:
                    EjecutarComandoDispensarEfectivo(cmd);
                    break;
                case ComandoDevolverTarjeta cmd:
                    EjecutarComandoDevolverTarjeta(cmd);
                    break;
                case ComandoImprimirRecibo cmd:
                    EjecutarComandoImprimirRecibo(cmd);
                    break;
                case ComandoMostrarInfoEnPantalla cmd:
                    EjecutarComandoMostrarInfoEnPantalla(cmd);
                    break;
                default:
                    throw new InvalidOperationException($"Comando {comando.GetType().Name} no soportado por el ATM");
            }

        consoleWriter.ForegroundColor = ConsoleColor.DarkYellow;
        consoleWriter.WriteLine("> Fin de la Transaccion\n\n");
        consoleWriter.ResetColor();
    }

    private void EjecutarComandoDispensarEfectivo(ComandoDispensarEfectivo comando)
    {
        threadSleeper.Sleep(1000);
        consoleWriter.ForegroundColor = ConsoleColor.Yellow;
        consoleWriter.Write("> Efectivo dispensado: ");
        consoleWriter.ResetColor();
        consoleWriter.WriteLine(comando.Monto);
        threadSleeper.Sleep(2000);
    }

    private void EjecutarComandoDevolverTarjeta(ComandoDevolverTarjeta comando)
    {
        threadSleeper.Sleep(500);
        consoleWriter.ForegroundColor = ConsoleColor.Yellow;
        consoleWriter.WriteLine("> Tarjeta devuelta");
        consoleWriter.ResetColor();
        threadSleeper.Sleep(1000);
    }

    private void EjecutarComandoImprimirRecibo(ComandoImprimirRecibo comando)
    {
        threadSleeper.Sleep(500);
        consoleWriter.ForegroundColor = ConsoleColor.Yellow;
        consoleWriter.WriteLine("> Imprimiento Recibo:");
        consoleWriter.ForegroundColor = ConsoleColor.Black;
        consoleWriter.BackgroundColor = ConsoleColor.White;
        var texto = "\t" + comando.TextoRecibo.Replace("\n", "\n\t"); // Poniendole sangría
        consoleWriter.Write(texto);
        consoleWriter.ResetColor();
        consoleWriter.WriteLine("");
        threadSleeper.Sleep(1500);
    }

    private void EjecutarComandoMostrarInfoEnPantalla(ComandoMostrarInfoEnPantalla comando)
    {
        consoleWriter.ForegroundColor = ConsoleColor.Yellow;
        consoleWriter.WriteLine("> Mostrando pantalla:");
        consoleWriter.ForegroundColor = comando.Error ? ConsoleColor.Red : ConsoleColor.White;
        consoleWriter.BackgroundColor = ConsoleColor.DarkBlue;
        var texto = "\t" + comando.TextoPantalla.Replace("\n", "\n\t"); // Poniendole sangría
        consoleWriter.Write(texto);
        consoleWriter.ResetColor();
        consoleWriter.WriteLine("");
        threadSleeper.Sleep(500);
    }


    private byte[] Encriptar(string textoPlano)
    {
        if (!Configurado)
            throw new InvalidOperationException("El ATM aún no está configurado correctamente");

        var llave = tpk.Skip(0).Take(TAMANO_LLAVE).ToArray();
        var iv = tpk.Skip(TAMANO_LLAVE).ToArray();
        using (var llaveAes = Aes.Create())
        {
            llaveAes.Key = llave;
            llaveAes.IV = iv;

            var encriptador = llaveAes.CreateEncryptor();

            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encriptador, CryptoStreamMode.Write))
                {
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(textoPlano);
                    }

                    return ms.ToArray();
                }
            }
        }
    }


}