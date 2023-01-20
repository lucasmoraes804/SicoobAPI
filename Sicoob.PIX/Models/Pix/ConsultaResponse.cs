﻿using Sicoob.Shared.Models.Geral;
using System;

namespace Sicoob.PIX.Models.Pix
{
    public class ConsultaResponse
    {
        public ResponseParametros parametros { get; set; }
        public PixResponse[] pix { get; set; }

    }
    public class PixResponse
    {
        public string endToEndId { get; set; }
        public string txid { get; set; }
        public decimal valor { get; set; }
        public string chave { get; set; }
        public DateTime horario { get; set; }
        public string nomePagador { get; set; }
        public NomeCpfCnpj pagador { get; set; }
        public Devolucao[] devolucoes { get; set; }

        public override string ToString()
        {
            string dev = "";
            if(devolucoes != null && devolucoes.Length > 0)
            {
                dev = $" [Dev:{dev.Length}]";
            }

            return $"{horario:g} {valor:C2} {nomePagador}{dev}";
        }

        public class Devolucao
        {
            public string id { get; set; }
            public string rtrId { get; set; }
            public string valor { get; set; }
            public DevolucaoHorario horario { get; set; }
            public string status { get; set; } // EM_PROCESSAMENTO, DEVOLVIDO, NAO_REALIZADO
            public string motivo { get; set; }
        }
        public class DevolucaoHorario
        {
            public DateTime solicitacao { get; set; }
            public DateTime liquidacao { get; set; }
        }
    }
}
