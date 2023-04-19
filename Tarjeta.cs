using System.Text.RegularExpressions;

namespace ATMSim;

public class Tarjeta
{

    public string NumeroCuenta { get; }
    public string Numero { get; }

    public Tarjeta(string numero, string numeroCuenta, bool contieneDigitoVerificador = false)
    {
        if (!Regex.Match(numero, @"[0-9]{15,19}").Success)
            throw new ArgumentException("Numero de tarjeta inválido");

        if (contieneDigitoVerificador)
        {
            if (!ValidarDigitoVerificador(numero))
                throw new ArgumentException("Dígito verificador inválido");
        }
        else
        {
            numero = numero + CalcularDigitoVerificacion(numero);
        }

        NumeroCuenta = numeroCuenta;
        Numero = numero;
    }

    public static string EnmascararNumero(string numeroTarjeta)
    {
        return numeroTarjeta[..6] + new string('*', numeroTarjeta.Length - 10) + numeroTarjeta[^4..];
    }


    public static int CalcularDigitoVerificacion(string numeroSinDigitoVerificador)
    {
        var sum = 0;
        var count = 1;
        for (var n = numeroSinDigitoVerificador.Length - 1; n >= 0; n -= 1)
        {
            var multiplo = count % 2 == 0 ? 1 : 2;
            var digito = (int)char.GetNumericValue(numeroSinDigitoVerificador[n]);
            var prod = digito * multiplo;
            prod = prod > 9 ? prod - 9 : prod;
            sum += prod;
            count++;
        }


        return 10 - sum % 10;
    }


    public static bool ValidarDigitoVerificador(string numero)
    {

        var numeroSinDigitoVerificador = numero[..^1];


        var digitoVerificadorAValidar =
            (int)char.GetNumericValue(numero[^1]);

        return CalcularDigitoVerificacion(numeroSinDigitoVerificador) == digitoVerificadorAValidar;
    }
}