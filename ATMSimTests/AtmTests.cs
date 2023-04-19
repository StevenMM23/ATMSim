using ATMSim;
using ATMSimTests.Fakes;
using FluentAssertions;
using System.Net.NetworkInformation;

namespace ATMSimTests
{
    public class AtmTests
    {
        const string teclasRetiroConRecibo = "AAA";
        const string teclasRetiroSinRecibo = "AAC";
        const string teclasConsultaDeBalance = "B";

        //Utilizando el Factory Method en Vez del anterior
        // Ahora utiliza ATMFactory e invoca el metodo CrearATM

        private static IATM CrearATM(string nombre, IConsoleWriter consoleWriter, IThreadSleeper threadSleeper)
            => new ATMFactory(consoleWriter, threadSleeper).CrearATM(nombre);

        private static string CrearCuentaYTarjeta(IAutorizador autorizador, TipoCuenta tipoCuenta, int balanceInicial, string binTarjeta, string pin)
        {
            string numeroCuenta = autorizador.CrearCuenta(tipoCuenta, balanceInicial);
            string numeroTarjeta = autorizador.CrearTarjeta(binTarjeta, numeroCuenta);
            autorizador.AsignarPin(numeroTarjeta, pin);
            return numeroTarjeta;
        }

        private static void RegistrarATMEnSwitch(IATM atm, IATMSwitch atmSwitch, IHSM hsm)
        {
            ComponentesLlave llaveATM = hsm.GenerarLlave();
            atm.InstalarLlave(llaveATM.LlaveEnClaro);
            atmSwitch.RegistrarATM(atm, llaveATM.LlaveEncriptada);
        }

        private static IAutorizador CrearAutorizador(string nombre, IHSM hsm) => new Autorizador(nombre, hsm);

        private static void RegistrarAutorizadorEnSwitch(IAutorizador autorizador, IATMSwitch atmSwitch, IHSM hsm)
        {
            ComponentesLlave llaveAutorizador = hsm.GenerarLlave();
            autorizador.InstalarLlave(llaveAutorizador.LlaveEncriptada);
            atmSwitch.RegistrarAutorizador(autorizador, llaveAutorizador.LlaveEncriptada);
            atmSwitch.AgregarRuta("459413", autorizador.Nombre);
        }

        private static IATMSwitch CrearSwitch(IHSM hsm, IConsoleWriter consoleWriter)
        {
            IATMSwitch atmSwitch = new ATMSwitch(hsm, consoleWriter);
            atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey()
            {
                Teclas = teclasRetiroConRecibo,
                TipoTransaccion = TipoTransaccion.Retiro,
                Recibo = true
            });
            atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey()
            {
                Teclas = teclasRetiroSinRecibo,
                TipoTransaccion = TipoTransaccion.Retiro,
                Recibo = false
            });
            atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey()
            {
                Teclas = teclasConsultaDeBalance,
                TipoTransaccion = TipoTransaccion.Consulta,
                Recibo = false
            });
            return atmSwitch;
        }


        [Fact]
        public void Withdrawal_with_balance_on_account_is_successful()
        {
            // ARRANGE
            FakeConsoleWriter consoleWriter = new FakeConsoleWriter();
            FakeThreadSleeper threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            IATMSwitch atmSwitch = CrearSwitch(hsm, consoleWriter);

            IATM sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            IAutorizador autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            string numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20_000, "459413", "1234");

            // ACT
            sut.EnviarTransactionRequest("AAA", numeroTarjeta, "1234", 100);

            // ASSERT
            consoleWriter.consoleText.Should().Contain("> Efectivo dispensado: 100");

        }

        #region Pruebas del Equipo 1

        //Prueba el Withdrawl con la Tecla sin Recibos AAC
        [Fact]
        public void Withdrawal_with_balance_on_account_is_successful_when_using_teclasRetiroSinRecibo()
        {
            // ARRANGE
            var consoleWriter = new FakeConsoleWriter();
            var threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            var atmSwitch = CrearSwitch(hsm, consoleWriter);

            var sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            var autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            var numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20_000, "459413", "1234");

            // ACT
            sut.EnviarTransactionRequest(teclasRetiroSinRecibo, numeroTarjeta, "1234", 100);

            // ASSERT
            consoleWriter.consoleText.Should().Contain("> Efectivo dispensado: 100");
        }

        //Se prueba que falle cuando no se tenga el PIN Indicado
        [Fact]
        public void Withdrawal_fails_when_using_invalid_PIN()
        {
            // ARRANGE
            var consoleWriter = new FakeConsoleWriter();
            var threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            var atmSwitch = CrearSwitch(hsm, consoleWriter);

            var sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            var autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            var numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20_000, "459413", "1234");

            // ACT
            sut.EnviarTransactionRequest("AAA", numeroTarjeta, "0000", 100);

            // ASSERT
            consoleWriter.consoleText.Should()
                .Contain("> Mostrando pantalla:\n\tPin incorrecto\n> Fin de la Transaccion\n\n\n");
        }

        //Se Prueba que falle cuando la cuenta no exista
        [Fact]
        public void Withdrawal_fails_when_account_does_not_exist()
        {
            // ARRANGE
            var consoleWriter = new FakeConsoleWriter();
            var threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            var atmSwitch = CrearSwitch(hsm, consoleWriter);

            var sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            var autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            var numeroTarjeta = CrearCuentaYTarjeta(autorizador, TipoCuenta.Ahorros, 20_000, "459413", "1234");

            // ACT
            sut.EnviarTransactionRequest("AAA", "1234567890", "1234", 100);

            // ASSERT
            consoleWriter.consoleText.Should()
                .Contain(
                    "Lo Sentimos");
        }

        [Fact]
        public void Balance_inquiry_displays_correct_balance_for_valid_card_and_PIN_combination()
        {
            // ARRANGE
            var consoleWriter = new FakeConsoleWriter();
            var threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            var atmSwitch = CrearSwitch(hsm, consoleWriter);

            var sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            var autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            var numeroCuenta = autorizador.CrearCuenta(TipoCuenta.Ahorros, 20_000);
            var numeroTarjeta = autorizador.CrearTarjeta("459413", numeroCuenta);
            autorizador.AsignarPin(numeroTarjeta, "1234");

            // ACT
            sut.EnviarTransactionRequest("B", numeroTarjeta, "1234");

            // ASSERT
            consoleWriter.consoleText.Should()
                .Contain("> Mostrando pantalla:\n\tSu balance actual es de: 20000\n> Fin de la Transaccion\n\n\n");
        }

        [Fact]
        public void Balance_inquiry_fails_when_using_invalid_PIN()
        {
            // ARRANGE
            var consoleWriter = new FakeConsoleWriter();
            var threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            var atmSwitch = CrearSwitch(hsm, consoleWriter);

            var sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            var autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            var numeroCuenta = autorizador.CrearCuenta(TipoCuenta.Ahorros, 20_000);
            var numeroTarjeta = autorizador.CrearTarjeta("459413", numeroCuenta);
            autorizador.AsignarPin(numeroTarjeta, "1234");

            // ACT
            sut.EnviarTransactionRequest("B", numeroTarjeta, "5678");

            // ASSERT
            consoleWriter.consoleText.Should()
                .Contain("> Mostrando pantalla:\n\tPin incorrecto\n> Fin de la Transaccion\n\n\n");
        }

        [Fact]
        public void Balance_inquiry_fails_when_card_is_not_registered_with_authorizer()
        {
            // ARRANGE
            var consoleWriter = new FakeConsoleWriter();
            var threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            var atmSwitch = CrearSwitch(hsm, consoleWriter);

            var sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            var autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            // ACT
            sut.EnviarTransactionRequest("B", "4594130000000000", "1234");

            // ASSERT
            consoleWriter.consoleText.Should()
                .Contain("> Mostrando pantalla:\n\tTarjeta no reconocida\n> Fin de la Transaccion\n\n\n");
        }

        [Fact]
        public void Balance_inquiry_fails_when_account_is_not_linked_to_card()
        {
            // ARRANGE
            var consoleWriter = new FakeConsoleWriter();
            var threadSleeper = new FakeThreadSleeper();

            IHSM hsm = new HSM();

            var atmSwitch = CrearSwitch(hsm, consoleWriter);

            var sut = CrearATM("AJP001", consoleWriter, threadSleeper);
            RegistrarATMEnSwitch(sut, atmSwitch, hsm);

            var autorizador = CrearAutorizador("AutDB", hsm);
            RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

            var numeroCuenta = autorizador.CrearCuenta(TipoCuenta.Ahorros, 20_000);
            var numeroTarjeta = autorizador.CrearTarjeta("459413", numeroCuenta);

            // ACT
            sut.EnviarTransactionRequest(teclasConsultaDeBalance, numeroTarjeta, "1234");

            // ASSERT
            consoleWriter.consoleText.Should()
                .Contain("> Mostrando pantalla:\n\tPin incorrecto\n> Fin de la Transaccion\n\n\n");
        }
        #endregion

    }
}