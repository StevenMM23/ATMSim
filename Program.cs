using ATMSim;

////////////////////////////////// CONSTANTS //////////////////////////////////


const string pin = "1234";
const string pinIncorrecto = "9999";
const string binTarjeta = "459413";
const double limiteSobregiro = 5000;
const double cantidadRetirar = 1000;
const TipoCuenta tipoDeCuenta = TipoCuenta.Ahorros;
const int balanceInicialCuenta = 10_000;

const string teclasRetiroConRecibo = "AAA";
const string teclasRetiroSinRecibo = "AAC";
const string teclasConsultaDeBalance = "B";

/////////////////////////////////////// MAIN //////////////////////////////////


IConsoleWriter consoleWriter = new ConsoleWriter();
IThreadSleeper threadSleeper = new ThreadSleeper();

var hsm = new HSM();
var atmSwitch = CrearSwitch(hsm, consoleWriter);

var atm = CrearATM("AJP001", consoleWriter, threadSleeper);
RegistrarATMEnSwitch(atm, atmSwitch, hsm);

var autorizador = CrearAutorizador("AutDB", hsm);
RegistrarAutorizadorEnSwitch(autorizador, atmSwitch, hsm);

var numeroTarjeta = CrearCuentaYTarjeta(autorizador, tipoDeCuenta, balanceInicialCuenta, binTarjeta, pin);

SecuenciaDeTransaccionesDeEjemplo(atm, numeroTarjeta);


//////////////////////////////// SETUP HELPER METHODS /////////////////////////

static IATM CrearATM(string nombre, IConsoleWriter consoleWriter, IThreadSleeper threadSleeper)
{
    return new ATM(nombre, consoleWriter, threadSleeper);
}

static string CrearCuentaYTarjeta(IAutorizador autorizador, TipoCuenta tipoCuenta, int balanceInicial,
    string binTarjeta, string pin)
{
    var numeroCuenta = autorizador.CrearCuenta(tipoCuenta, balanceInicial, limiteSobregiro);
    var numeroTarjeta = autorizador.CrearTarjeta(binTarjeta, numeroCuenta);
    autorizador.AsignarPin(numeroTarjeta, pin);
    return numeroTarjeta;
}

static void RegistrarATMEnSwitch(IATM atm, IATMSwitch atmSwitch, IHSM hsm)
{
    var llaveATM = hsm.GenerarLlave();
    atm.InstalarLlave(llaveATM.LlaveEnClaro);
    atmSwitch.RegistrarATM(atm, llaveATM.LlaveEncriptada);
}

static IAutorizador CrearAutorizador(string nombre, IHSM hsm)
{
    return new Autorizador(nombre, hsm);
}

static void RegistrarAutorizadorEnSwitch(IAutorizador autorizador, IATMSwitch atmSwitch, IHSM hsm)
{
    var llaveAutorizador = hsm.GenerarLlave();
    autorizador.InstalarLlave(llaveAutorizador.LlaveEncriptada);
    atmSwitch.RegistrarAutorizador(autorizador, llaveAutorizador.LlaveEncriptada);
    atmSwitch.AgregarRuta("459413", autorizador.Nombre);
}


static IATMSwitch CrearSwitch(IHSM hsm, IConsoleWriter consoleWriter)
{
    IATMSwitch atmSwitch = new ATMSwitch(hsm, consoleWriter);
    atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey
    {
        Teclas = teclasRetiroConRecibo,
        TipoTransaccion = TipoTransaccion.Retiro,
        Recibo = true
    });
    atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey
    {
        Teclas = teclasRetiroSinRecibo,
        TipoTransaccion = TipoTransaccion.Retiro,
        Recibo = false
    });
    atmSwitch.AgregarConfiguracionOpKey(new ConfiguracionOpKey
    {
        Teclas = teclasConsultaDeBalance,
        TipoTransaccion = TipoTransaccion.Consulta,
        Recibo = false
    });
    return atmSwitch;
}

//////////////////////////////// DEMO SEQUENCE /////////////////////////

static void SecuenciaDeTransaccionesDeEjemplo(IATM atm, string numeroTarjeta)
{
    EsperarTeclaEnter("Presione ENTER para realizar una consulta de balance");
    atm.EnviarTransactionRequest(teclasConsultaDeBalance, numeroTarjeta, pin);

    EsperarTeclaEnter($"Presione ENTER para realizar un retiro de {cantidadRetirar} sin impresión de recibo");
    atm.EnviarTransactionRequest(teclasRetiroSinRecibo, numeroTarjeta, pin, cantidadRetirar);



    EsperarTeclaEnter($"Presione ENTER para realizar un intento retiro {cantidadRetirar} pero con pin incorrecto");
    atm.EnviarTransactionRequest(teclasRetiroConRecibo, numeroTarjeta, pinIncorrecto, cantidadRetirar);

    EsperarTeclaEnter("Presione ENTER para realizar una consulta de balance");
    atm.EnviarTransactionRequest(teclasConsultaDeBalance, numeroTarjeta, pin);

    EsperarTeclaEnter("Presione ENTER para realizar un retiro de 6,500 con recibo");
    atm.EnviarTransactionRequest(teclasRetiroConRecibo, numeroTarjeta, pin, 6_500);

    EsperarTeclaEnter(
        $"Presione ENTER para realizar un intento de retiro de 4_000 que declinará por fondos insuficientes"); //Recordar que el resultado de este codigo dependera si es una cuenta de Ahorro o de Corriente y las de corrientes tienen un limite en el sobregiro realizado

    atm.EnviarTransactionRequest(teclasRetiroConRecibo, numeroTarjeta, pin, 4_000);

    EsperarTeclaEnter($"Presione ENTER para realizar un retiro de 12,000 sin impresión de recibo");
    atm.EnviarTransactionRequest(teclasConsultaDeBalance, numeroTarjeta, pin);

    EsperarTeclaEnter("Presione ENTER para finalizar");
}

static void EsperarTeclaEnter(string mensaje)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine(mensaje);
    Console.ReadKey();
    Console.ResetColor();
}