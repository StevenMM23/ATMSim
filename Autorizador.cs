using System.Text.RegularExpressions;

namespace ATMSim;

public interface IAutorizador
{
    public RespuestaConsultaDeBalance ConsultarBalance(string numeroTarjeta, byte[] criptogramaPin);

    public RespuestaRetiro AutorizarRetiro(string numeroTarjeta, double montoRetiro, byte[] criptogramaPin,
        int limiteRetiro = 0);

    public string CrearTarjeta(string bin, string numeroCuenta);
    public string CrearCuenta(TipoCuenta tipo, double montoDeApertura = 0, double montoLimiteSobregiro = 0);
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
    public double? LimiteRetiro { get; }

    public RespuestaRetiro(int codigoRespuesta, double? montoAutorizado = null, double? balanceLuegoDelRetiro = null,
        double limiteRetiro = 0) : base(
        codigoRespuesta)
    {
        MontoAutorizado = montoAutorizado;
        BalanceLuegoDelRetiro = balanceLuegoDelRetiro.HasValue
            ? double.Parse(balanceLuegoDelRetiro.Value.ToString("F2"))
            : null;
        LimiteRetiro = limiteRetiro;
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

    private bool PermitirSobregiro(double montoSobregirado, double limite)
    {
        return montoSobregirado < limite;
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


    // Consolidate Conditional Expression and Extract Method Refactoring 
    #region Extract Method and Consolidate Conditional Expression

    #region Codigo Nuevo

    private bool TarjetaValida(string numeroTarjeta)
    {
        return TarjetaExiste(numeroTarjeta) && TarjetaTienePin(numeroTarjeta);
    }

    public RespuestaConsultaDeBalance ConsultarBalance(string numeroTarjeta, byte[] criptogramaPin)
    {
        if (!TarjetaValida(numeroTarjeta))
            return new RespuestaConsultaDeBalance(!TarjetaExiste(numeroTarjeta) ? 56 : 55);

        var criptogramaPinReal = ObtenerCriptogramaPinTarjeta(numeroTarjeta);

        if (criptogramaLlaveAutorizador != null &&
            !hsm.ValidarPin(criptogramaPin, criptogramaLlaveAutorizador, criptogramaPinReal))
            return new RespuestaConsultaDeBalance(55); // Pin incorrecto

        var tarjeta = ObtenerTarjeta(numeroTarjeta);
        var cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);

        return new RespuestaConsultaDeBalance(0, cuenta.Monto); // Autorizado
    }

    public RespuestaRetiro AutorizarRetiro(string numeroTarjeta, double montoRetiro, byte[] criptogramaPin,
        int limiteRetiro = 0)
    {
        if (!TarjetaValida(numeroTarjeta)) return new RespuestaRetiro(!TarjetaExiste(numeroTarjeta) ? 56 : 55);

        var criptogramaPinReal = ObtenerCriptogramaPinTarjeta(numeroTarjeta);

        if (criptogramaLlaveAutorizador != null &&
            !hsm.ValidarPin(criptogramaPin, criptogramaLlaveAutorizador, criptogramaPinReal))
            return new RespuestaRetiro(55); // Pin incorrecto

        var tarjeta = ObtenerTarjeta(numeroTarjeta);
        var cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);

        if (cuenta.Tipo == TipoCuenta.Ahorros &&
            cuenta.Monto < montoRetiro) return new RespuestaRetiro(51); // Fondos Insuficientes

        if (tarjeta.Bloqueada) return new RespuestaRetiro(57); // Su Tarjeta esta bloqueada

        cuenta.Monto -= montoRetiro;

        if (cuenta.Tipo != TipoCuenta.Corriente || !PermitirSobregiro(cuenta.Monto, cuenta.LimiteSobregiro * -1))
            return new RespuestaRetiro(0, montoRetiro, cuenta.Monto); // Autorizado
        cuenta.Monto += montoRetiro;
        return new RespuestaRetiro(52, montoRetiro, cuenta.Monto, cuenta.LimiteSobregiro);
    }

    #endregion

    #region Codigo Antiguo

    // public RespuestaConsultaDeBalance ConsultarBalance(string numeroTarjeta, byte[] criptogramaPin)
    // {
    //     if (!TarjetaExiste(numeroTarjeta))
    //         return new RespuestaConsultaDeBalance(56); // Esta tarjeta no se reconoce
    //
    //     if (!TarjetaTienePin(numeroTarjeta))
    //         return new RespuestaConsultaDeBalance(55); // Esta tarjeta no tiene pin asignado
    //
    //     var criptogramaPinReal = ObtenerCriptogramaPinTarjeta(numeroTarjeta);
    //
    //     if (!hsm.ValidarPin(criptogramaPin, criptogramaLlaveAutorizador, criptogramaPinReal))
    //         return new RespuestaConsultaDeBalance(55); // Pin incorrecto
    //
    //     var tarjeta = ObtenerTarjeta(numeroTarjeta);
    //     var cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);
    //
    //     return new RespuestaConsultaDeBalance(0, cuenta.Monto); // Autorizado
    // }
    //
    //
    //
    //
    // public RespuestaRetiro AutorizarRetiro(string numeroTarjeta, double montoRetiro, byte[] criptogramaPin,
    //     int limiteRetiro = 0)
    // {
    //     if (!TarjetaExiste(numeroTarjeta))
    //         return new RespuestaRetiro(56); // Esta tarjeta no se reconoce
    //
    //     if (!TarjetaTienePin(numeroTarjeta))
    //         return new RespuestaRetiro(55); // Esta tarjeta no tiene pin asignado
    //
    //     var criptogramaPinReal = ObtenerCriptogramaPinTarjeta(numeroTarjeta);
    //
    //     if (!hsm.ValidarPin(criptogramaPin, criptogramaLlaveAutorizador, criptogramaPinReal))
    //         return new RespuestaRetiro(55); // Pin incorrecto
    //
    //     var tarjeta = ObtenerTarjeta(numeroTarjeta);
    //     var cuenta = ObtenerCuenta(tarjeta.NumeroCuenta);
    //
    //     if (cuenta.Tipo == TipoCuenta.Ahorros &&
    //         cuenta.Monto < montoRetiro) return new RespuestaRetiro(51); // Fondos Insuficientes
    //
    //
    //     if (tarjeta.Bloqueada) return new RespuestaRetiro(57); // Su Tarjeta esta bloqueada
    //
    //     cuenta.Monto -= montoRetiro;
    //     if (cuenta.Tipo == TipoCuenta.Corriente && PermitirSobregiro(cuenta.Monto, cuenta.LimiteSobregiro * -1))
    //     {
    //         cuenta.Monto += montoRetiro;
    //         // No se permite sobregiros
    //         return new RespuestaRetiro(52, montoRetiro, cuenta.Monto, cuenta.LimiteSobregiro);
    //     }
    //
    //     return new RespuestaRetiro(0, montoRetiro, cuenta.Monto); // Autorizado
    // }

    #endregion

    #endregion

    public void InstalarLlave(byte[] criptogramaLlaveAutorizador)
    {
        this.criptogramaLlaveAutorizador = criptogramaLlaveAutorizador;
    }

    //Extract Method - ValidarBin, ValidarCuenta, ValidarPrefijoSufijo
    #region Extract Method

    #region Codigo Nuevo

    public string CrearTarjeta(string bin, string numeroCuenta)
    {
        ValidarBin(bin);

        if (cuentas.All(x => x.Numero != numeroCuenta))
            throw new NotImplementedException("Número de cuenta no encontrado");

        ValidarNumeroCuenta(numeroCuenta);

        string numeroSinDigitoVerificador;
        do
        {
            // repetir hasta encontrar un número único (sin tomar en cuenta el digito verificador)
            numeroSinDigitoVerificador = GenerarNumeroAleatorio(tamanoNumeroTarjeta - 1, bin);
        } while (tarjetas.Any(x => x.Numero[..^1] == numeroSinDigitoVerificador));

        var tarjeta = new Tarjeta(numeroSinDigitoVerificador, numeroCuenta);
        tarjetas.Add(tarjeta);

        return tarjeta.Numero;
    }

    public string CrearCuenta(TipoCuenta tipo, double montoDeApertura = 0, double limiteDeSobregiro = 0)
    {
        string numero;
        do
        {
            // repetir hasta encontrar un número único
            numero = GenerarNumeroAleatorio(tamanoNumeroCuenta, prefijoDeCuenta);
        } while (CuentaExiste(numero));

        var cuenta = new Cuenta(numero, tipo, montoDeApertura, limiteDeSobregiro);
        cuentas.Add(cuenta);

        return cuenta.Numero;
    }

    private string GenerarNumeroAleatorio(int cantidadPosiciones, string prefijo = "", string sufijo = "")
    {
        const string digitos = "0123456789";

        ValidarPrefijoSufijo(prefijo, sufijo);

        if (cantidadPosiciones <= prefijo.Length + sufijo.Length)
            throw new ArgumentException("Debe haber al menos una posición que no sean parte del prefijo/sufijo");

        // Arreglar el length
        var numero = new string(Enumerable.Repeat(digitos, cantidadPosiciones - prefijo.Length - sufijo.Length)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
        return prefijo + numero + sufijo;
    }

    private void ValidarBin(string bin)
    {
        if (!Regex.Match(bin, @"[0-9]{6}").Success)
            throw new ArgumentException("El Bin debe ser numérico, de 6 dígitos");

        if (bin[0] != '4')
            throw new NotImplementedException("Sólo se soportan tarjertas VISA, que inician con 4");
    }

    private void ValidarNumeroCuenta(string numeroCuenta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
            throw new ArgumentException("El número de cuenta no puede estar vacío");

        if (!Regex.IsMatch(numeroCuenta, "^[0-9]+$"))
            throw new ArgumentException("El número de cuenta debe contener sólo caracteres numéricos");
    }
    private void ValidarPrefijoSufijo(string prefijo, string sufijo)
    {
        if (!Regex.Match(prefijo + sufijo, @"[0-9]+").Success)
            throw new ArgumentException("El Sufijo y el Prefijo deben ser caracteres numéricos");
    }
    #endregion

    #region Codigo Antiguo

    // public string CrearTarjeta(string bin, string numeroCuenta)
    // {
    //     if (!Regex.Match(bin, @"[0-9]{6}").Success)
    //         throw new ArgumentException("El Bin debe ser numérico, de 6 dígitos");
    //
    //     if (bin[0] != '4')
    //         throw new NotImplementedException("Sólo se soportan tarjertas VISA, que inician con 4");
    //
    //     if (cuentas.All(x => x.Numero != numeroCuenta))
    //         throw new NotImplementedException("Número de cuenta no encontrado");
    //
    //     string numeroSinDigitoVerificador;
    //     do
    //     {
    //         // repetir hasta encontrar un número único (sin tomar en cuenta el digito verificador)
    //         numeroSinDigitoVerificador = GenerarNumeroAleatorio(tamanoNumeroTarjeta - 1, bin);
    //     } while (tarjetas.Any(x => x.Numero[..^1] == numeroSinDigitoVerificador));
    //
    //     var tarjeta = new Tarjeta(numeroSinDigitoVerificador, numeroCuenta);
    //     tarjetas.Add(tarjeta);
    //
    //     return tarjeta.Numero;
    // }
    //
    // public string CrearCuenta(TipoCuenta tipo, double montoDeApertura = 0, double limiteDeSobregiro = 0)
    // {
    //     string numero;
    //     do
    //     {
    //         // repetir hasta encontrar un número único
    //         numero = GenerarNumeroAleatorio(tamanoNumeroCuenta, prefijoDeCuenta);
    //     } while (CuentaExiste(numero));
    //
    //     var cuenta = new Cuenta(numero, tipo, montoDeApertura, limiteDeSobregiro);
    //     cuentas.Add(cuenta);
    //
    //     return cuenta.Numero;
    // }
    //
    // private string GenerarNumeroAleatorio(int cantidadPosiciones, string prefijo = "", string sufijo = "")
    // {
    //     const string digitos = "0123456789";
    //
    //     if (!Regex.Match(prefijo + sufijo, @"[0-9]+").Success)
    //         throw new ArgumentException("El Sufijo y el Prefijo deben ser caracteres numéricos");
    //
    //     if (cantidadPosiciones <= prefijo.Length + sufijo.Length)
    //         throw new ArgumentException("Debe haber al menos una posición que no sean parte del prefijo/sufijo");
    //
    //     // Arreglar el length
    //     var numero = new string(Enumerable.Repeat(digitos, cantidadPosiciones - prefijo.Length - sufijo.Length)
    //         .Select(s => s[random.Next(s.Length)])
    //         .ToArray());
    //     return prefijo + numero + sufijo;
    // }

    #endregion

    #endregion
}