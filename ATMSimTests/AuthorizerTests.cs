using ATMSim;
using ATMSimTests.Fakes;
using FluentAssertions;
using System.Security.Cryptography;

namespace ATMSimTests
{
    public class AuthorizerTests
    {


        private static string CrearCuentaYTarjeta(IAutorizador autorizador, TipoCuenta tipoCuenta, int balanceInicial, string binTarjeta, string pin)
        {
            string numeroCuenta = autorizador.CrearCuenta(tipoCuenta, balanceInicial);
            string numeroTarjeta = autorizador.CrearTarjeta(binTarjeta, numeroCuenta);
            autorizador.AsignarPin(numeroTarjeta, pin);
            return numeroTarjeta;
        }

        private static IAutorizador CrearAutorizador(string nombre, IHSM hsm) => new Autorizador(nombre, hsm);

        public byte[] Encriptar(string textoPlano, byte[] llaveEnClaro)
        {
            const int TAMANO_LLAVE = 32;

            byte[] llave = llaveEnClaro.Skip(0).Take(TAMANO_LLAVE).ToArray();
            byte[] iv = llaveEnClaro.Skip(TAMANO_LLAVE).ToArray();
            using (Aes llaveAes = Aes.Create())
            {
                llaveAes.Key = llave;
                llaveAes.IV = iv;

                ICryptoTransform encriptador = llaveAes.CreateEncryptor();

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


        }

        [Fact]
        public void Accounts_of_type_checking_allow_overdraft()
        {
            // ARRANGE
            IHSM hsm = new HSM();
            IAutorizador sut = CrearAutorizador("Autorizador", hsm);
            ComponentesLlave llave = hsm.GenerarLlave();
            sut.InstalarLlave(llave.LlaveEncriptada);
            string numeroTarjeta = CrearCuentaYTarjeta(sut, TipoCuenta.Corriente, 10_000, "455555", "1234");
            byte[] criptogramaPin = Encriptar("1234", llave.LlaveEnClaro);

            // ACT
            RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroTarjeta, 15_500, criptogramaPin);

            // ASSERT
            respuesta.MontoAutorizado.Should().Be(15_500);
            respuesta.BalanceLuegoDelRetiro.Should().Be(-5_500);
            respuesta.CodigoRespuesta.Should().Be(0);

        }

        [Fact]
        public void Balance_Inquiry_with_incorrect_pin_return_respcode_55()
        {
            // ARRANGE
            IHSM hsm = new HSM();
            IAutorizador sut = CrearAutorizador("Autorizador", hsm);
            ComponentesLlave llave = hsm.GenerarLlave();
            sut.InstalarLlave(llave.LlaveEncriptada);
            string numeroTarjeta = CrearCuentaYTarjeta(sut, TipoCuenta.Corriente, 10_000, "455555", "1234");

            byte[] criptogramaPinIncorrecto = Encriptar("9999", llave.LlaveEnClaro);

            // ACT
            RespuestaConsultaDeBalance respuesta = sut.ConsultarBalance(numeroTarjeta, criptogramaPinIncorrecto);

            // ASSERT
            respuesta.CodigoRespuesta.Should().Be(55);
            respuesta.BalanceActual.Should().BeNull();

        }

        #region Pruebas Creada por el Equipo #1

        //Comprueba que la cuenta que se esta creando tiene el saldo que le fue asignado desde el principio
        [Fact]
        public void Account_creation_with_initial_balance_should_increase_balance()
        {
            // ARRANGE
            IHSM hsm = new HSM();
            IAutorizador sut = CrearAutorizador("Autorizador", hsm);
            var llave = hsm.GenerarLlave();
            sut.InstalarLlave(llave.LlaveEncriptada);

            var balanceActual = 10_000;
            string numeroCuenta = CrearCuentaYTarjeta(sut, TipoCuenta.Ahorros, balanceActual, "455555", "1234");
            byte[] criptogramaPin = Encriptar("1234", llave.LlaveEnClaro);
            // ACT

            var respuestaAhorro = sut.ConsultarBalance(numeroCuenta, criptogramaPin);


            // ASSERT
            respuestaAhorro.BalanceActual.Should().Be(10_000);

        }


        //Comprueba que las cuentas de Ahorro no son capaz de hacer sobregiros
        [Fact]
        public void Withdrawal_over_balance_should_decline_for_savings_account()
        {
            // ARRANGE
            IHSM hsm = new HSM();
            IAutorizador sut = CrearAutorizador("Autorizador", hsm);
            ComponentesLlave llave = hsm.GenerarLlave();
            sut.InstalarLlave(llave.LlaveEncriptada);

            string numeroCuenta = CrearCuentaYTarjeta(sut, TipoCuenta.Ahorros, 10_000, "455555", "1234");
            byte[] criptogramaPin = Encriptar("1234", llave.LlaveEnClaro);

            // ACT
            RespuestaRetiro respuesta = sut.AutorizarRetiro(numeroCuenta, 15_500, criptogramaPin);


            // ASSERT
            respuesta.CodigoRespuesta.Should().Be(51);


        }

        //Comprueba que el pin asignado fue exitoso y que si se ingresa correctamente le dara una respuesta del balance
        [Fact]
        public void Pin_assignment_should_succeed()
        {
            // ARRANGE
            IHSM hsm = new HSM();
            IAutorizador sut = CrearAutorizador("Autorizador", hsm);
            ComponentesLlave llave = hsm.GenerarLlave();
            sut.InstalarLlave(llave.LlaveEncriptada);

            string numeroCuenta = CrearCuentaYTarjeta(sut, TipoCuenta.Corriente, 10_000, "455555", "0000");

            // ACT
            sut.AsignarPin(numeroCuenta, "1234");
            byte[] criptogramaPin = Encriptar("1234", llave.LlaveEnClaro);
            RespuestaConsultaDeBalance respuesta = sut.ConsultarBalance(numeroCuenta, criptogramaPin);

            // ASSERT
            respuesta.BalanceActual.Should().Be(10_000);
        }

        //Se prueba si se es capaz de asignar un Pin a una Tarjeta que no es valida
        [Fact]
        public void Pin_assignment_with_invalid_tarjeta_should_fail()
        {
            // ARRANGE
            IHSM hsm = new HSM();
            IAutorizador sut = CrearAutorizador("Autorizador", hsm);
            ComponentesLlave llave = hsm.GenerarLlave();
            sut.InstalarLlave(llave.LlaveEncriptada);

            string numeroCuenta = CrearCuentaYTarjeta(sut, TipoCuenta.Corriente, 10_000, "455555", "0000");
            string invalidTarjeta = "9999999999";

            // ACT
            Action asignarPin = () => sut.AsignarPin(invalidTarjeta, "1234");

            // ASSERT
            asignarPin.Should().Throw<ArgumentException>();
        }
    }
    #endregion
}

