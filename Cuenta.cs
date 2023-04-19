using System;
using System.Text.RegularExpressions;

namespace ATMSim
{
    public class IntentoSobregiroCuentaDeAhorrosException : Exception { }

    public enum TipoCuenta
    {
        Ahorros,
        Corriente
    }

    public class Cuenta
    {
        public TipoCuenta Tipo { get; }
        public string Numero { get; }

        private int monto;

        public int Monto
        {
            get => monto;
            set
            {
                if (Tipo == TipoCuenta.Ahorros && value < 0)
                {
                    throw new IntentoSobregiroCuentaDeAhorrosException();
                }

                monto = value;
            }
        }

        public Cuenta(string numero, TipoCuenta tipo, int monto = 0)
        {
            if (!Regex.IsMatch(numero, @"^[0-9]+$"))
            {
                throw new ArgumentException("Numero de cuenta inválido");
            }

            Numero = numero;
            Tipo = tipo;
            Monto = monto;
        }
    }
}