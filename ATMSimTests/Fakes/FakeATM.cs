using ATMSim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ATMSimTests.Fakes
{
    internal class FakeATM : IATM
    {
        public byte[] Llave { get; set; }

        public IATMSwitch? Switch { get; set; }
        public string Nombre { get; set; }

        public bool Configurado => throw new NotImplementedException();

        public FakeATM(string nombre) => Nombre = nombre;

        public void EnviarTransactionRequest(string opKeyBuffer, string numeroTarjeta, string pin, double monto = 0)
        {
            throw new NotImplementedException();
        }

        public void InstalarLlave(byte[] llave)
        {
            Llave = llave;
        }

        public void Reestablecer()
        {
            throw new NotImplementedException();
        }
    }
}
