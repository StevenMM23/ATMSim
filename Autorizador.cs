using System.Text.RegularExpressions;

namespace ATMSim;

public interface IAutorizador
{
    public RespuestaConsultaDeBalance ConsultarBalance(string numeroTarjeta, byte[] criptogramaPin);
    public RespuestaRetiro AutorizarRetiro(string numeroTarjeta, double montoRetiro, byte[] criptogramaPin);
    public string CrearTarjeta(string bin, string numeroCuenta);
    public string CrearCuenta(TipoCuenta tipo, double montoDeApertura = 0);
    public string Nombre { get; }
    public void AsignarPin(string numeroTarjeta, string pin);
    public void InstalarLlave(byte[] criptogramaLlaveAutorizador);
}

public class Respuesta
{
    public double CodigoRespuesta { get; }

    public Respuesta(double codigoRespuesta)
    {
        CodigoRespuesta = codigoRespuesta;
    }
}

public class RespuestaConsultaDeBalance : Respuesta
{
    public double? BalanceActual { get; }

    public RespuestaConsultaDeBalance(double codigoRespuesta, double? balanceActual = null) : base(codigoRespuesta)
    {
        BalanceActual = balanceActual.HasValue ? double.Parse(balanceActual.Value.ToString("F2")) : null;
    }
}

public class RespuestaRetiro : Respuesta
{
    public double? MontoAutorizado { get; }
    public double? BalanceLuegoDelRetiro { get; }

    public RespuestaRetiro(int codigoRespuesta, double? montoAutorizado = null, double? balanceLuegoDelRetiro = null) : base(
        codigoRespuesta)
    {
        MontoAutorizado = montoAutorizado;
        BalanceLuegoDelRetiro = balanceLuegoDelRetiro.HasValue ? double.Parse(balanceLuegoDelRetiro.Value.ToString("F2")) : null;
    }
}

public class Autorizador : IAutorizador
{
    private const int tamanoNumeroTarjeta = 16;
    private const int tamanoNumeroCuenta = 9;
    private const string prefijoDeCuenta = "7";

    private readonly Random random = new();
    public string Nombre { get; }

    private readonly IHSM hsm;

    private readonly List<Tarjeta> tarjetas = new();
    private readonly List<Cuenta> cuentas = new();

    private readonly Dictionary<string, byte[]> pinesTarjetas = new();

    private byte[]? criptogramaLlaveAutorizador;

    public Autorizador(string nombre, IHSM hsm)
    {
        Nombre = nombre;
        this.hsm = hsm;
    }

    public void AsignarPin(string numeroTarjeta, string pin)
    {
        if (!Regex.Match(pin, @"[0-9]{4}").Success)
            throw new ArgumentException("El Pin debe ser numérico, de 4 dígitos");

        if (!TarjetaExiste(numeroTarjeta))
            throw new ArgumentException("Número de tarjeta no reconocido");

        var criptogramaPin = hsm.EncriptarPinConLlaveMaestra(pin);

        pinesTarjetas[numeroTarjeta] = criptogramaPin;
    }

    private bool TarjetaExiste(string numeroTarjeta)
    {
        return tarjetas.Any(x => x.Numero == numeroTarjeta);
    }

    private Tarjeta ObtenerTarjeta(string numeroTarjeta)
    {
        return tarjetas.Single(x => x.Numero == numeroTarjeta);
    }

    private byte[] ObtenerCriptogramaPinTarjeta(string numeroTarjeta)
    {
        return pinesTarjetas[numeroTarjeta];
    }

    private bool TarjetaTienePin(string numeroTarjeta)
    {
        return pinesTarjetas.ContainsKey(numeroTarjeta);
    }

    private bool CuentaExiste(string numeroCuenta)
    {
        return cuentas.Any(x => x.Numero == numeroCuenta);
    }

    private Cuenta ObtenerCuenta(string numeroCuenta)
    {
        return cuentas.Single(x => x.Numero == numeroCuenta);
    }

    public RespuestaConsultaDeBalance ConsultarBalance(string numeroTarjeta, byte[] criptogramaPin)
    {
        if (!TarjetaExiste(numeroTarjeta))
            return new RespuestaConsultaDeBalance(56); // Esta tarjeta no se reconoce

        if (!TarjetaTienePin(numeroTarjeta))
            return new RespuestaConsultaDeBalance(55); // Esta tarjeta no tiene pin asignado

        var criptogramaPinReal = ObtenerCriptogramaPinTarjeta(numeroTarjeta);

        if (!hsm.ValidarPin(criptogramaPin, criptogramaLlaveAutorizador, criptogramaPinReal))
            return new RespuestaConsultaDeBalance(55); // Pin incorrecto

        var tarjeta = ObtenerTarjeta(numeroTarjeta);
        var cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);

        return new RespuestaConsultaDeBalance(0, cuenta.Monto); // Autorizado
    }

    public RespuestaRetiro AutorizarRetiro(string numeroTarjeta, double montoRetiro, byte[] criptogramaPin)
    {
        if (!TarjetaExiste(numeroTarjeta))
            return new RespuestaRetiro(56); // Esta tarjeta no se reconoce

        if (!TarjetaTienePin(numeroTarjeta))
            return new RespuestaRetiro(55); // Esta tarjeta no tiene pin asignado

        var criptogramaPinReal = ObtenerCriptogramaPinTarjeta(numeroTarjeta);

        if (!hsm.ValidarPin(criptogramaPin, criptogramaLlaveAutorizador, criptogramaPinReal))
            return new RespuestaRetiro(55); // Pin incorrecto

        var tarjeta = ObtenerTarjeta(numeroTarjeta);
        var cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);

        if (cuenta.Tipo == TipoCuenta.Ahorros && cuenta.Monto < montoRetiro)
        {
            return new RespuestaRetiro(51); // Fondos Insuficientes
        }

        cuenta.Monto -= montoRetiro;
        return new RespuestaRetiro(0, montoRetiro, cuenta.Monto); // Autorizado
    }

    public void InstalarLlave(byte[] criptogramaLlaveAutorizador)
    {
        this.criptogramaLlaveAutorizador = criptogramaLlaveAutorizador;
    }

    public string CrearTarjeta(string bin, string numeroCuenta)
    {
        if (!Regex.Match(bin, @"[0-9]{6}").Success)
            throw new ArgumentException("El Bin debe ser numérico, de 6 dígitos");

        if (bin[0] != '4')
            throw new NotImplementedException("Sólo se soportan tarjertas VISA, que inician con 4");

        if (!cuentas.Where(x => x.Numero == numeroCuenta).Any())
            throw new NotImplementedException("Número de cuenta no encontrado");

        string numeroSinDigitoVerificador;
        do
        {
            // repetir hasta encontrar un número único (sin tomar en cuenta el digito verificador)
            numeroSinDigitoVerificador = GenerarNumeroAleatorio(tamanoNumeroTarjeta - 1, bin);
        } while (tarjetas.Where(x => x.Numero[..^1] == numeroSinDigitoVerificador).Any());

        var tarjeta = new Tarjeta(numeroSinDigitoVerificador, numeroCuenta);
        tarjetas.Add(tarjeta);

        return tarjeta.Numero;
    }

    public string CrearCuenta(TipoCuenta tipo, double montoDeApertura = 0)
    {
        string numero;
        do
        {
            // repetir hasta encontrar un número único
            numero = GenerarNumeroAleatorio(tamanoNumeroCuenta, prefijoDeCuenta);
        } while (CuentaExiste(numero));

        var cuenta = new Cuenta(numero, tipo, montoDeApertura);
        cuentas.Add(cuenta);

        return cuenta.Numero;
    }

    private string GenerarNumeroAleatorio(int cantidadPosiciones, string prefijo = "", string sufijo = "")
    {
        const string digitos = "0123456789";

        if (!Regex.Match(prefijo + sufijo, @"[0-9]+").Success)
            throw new ArgumentException("El Sufijo y el Prefijo deben ser caracteres numéricos");

        if (cantidadPosiciones <= prefijo.Length + sufijo.Length)
            throw new ArgumentException("Debe haber al menos una posición que no sean parte del prefijo/sufijo");

        // Arreglar el length
        var numero = new string(Enumerable.Repeat(digitos, cantidadPosiciones - prefijo.Length - sufijo.Length)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
        return prefijo + numero + sufijo;
    }
}