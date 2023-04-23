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
        public double LimiteSobregiro { get; }
        private double monto;

        public double Monto
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

        public Cuenta(string numero, TipoCuenta tipo, double monto = 0, double limiteSobregiro = 0)
        {
            if (!Regex.IsMatch(numero, @"^[0-9]+$"))
            {
                throw new ArgumentException("Numero de cuenta inválido");
            }
            if (limiteSobregiro < 0)
            {
                throw new ArgumentException("Solo se permiten cantidades positivas de sobregiro");
            }

            Numero = numero;
            Tipo = tipo;
            Monto = monto;
            LimiteSobregiro = limiteSobregiro;
        }
    }
}