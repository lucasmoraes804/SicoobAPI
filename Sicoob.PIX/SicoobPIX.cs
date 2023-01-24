﻿using Sicoob.PIX.Models.Webhook;
using Sicoob.Shared;
using Sicoob.Shared.Models.Acesso;
using Simple.API;
using System;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sicoob.PIX
{
    public sealed class SicoobPIX : Shared.Sicoob
    {
        // Documentações
        // > APIs tipo "Swagger":
        //   https://developers.sicoob.com.br/#!/apis
        // > APIs no POSTMAN:
        //   https://documenter.getpostman.com/view/20565799/UzBnrmod#239d9f68-d646-4209-994a-cd564b6d6d1a
        // Auxiliares
        // > Parseia BR Code para debugar:
        //   https://openpix.com.br/qrcode/scanner/
        // > Gera QR para teste:
        //   https://webqr.com/create.html

        private ClientInfo clientApi;
        public Shared.Models.ConfiguracaoAPI ConfigApi { get; }

        private static readonly string rxTxidPattern = "^[a-zA-Z0-9]{26,35}$";
        private static readonly Regex rxTxid = new Regex(rxTxidPattern, RegexOptions.Compiled);

        public SicoobPIX(Shared.Models.ConfiguracaoAPI configApi)
            : base(configApi)
        {
            ConfigApi = configApi;
        }

        protected override void setupClients(HttpClientHandler handler)
        {
            clientApi = new ClientInfo(ConfigApi.UrlApi, handler);
            clientApi.SetHeader("x-sicoob-clientid", ConfigApi.ClientId);

#if DEBUG
            enableDebug(clientApi);
#endif

        }

#if DEBUG
        private void enableDebug(ClientInfo clientApi)
        {
            clientApi.BeforeSend += ClientApi_BeforeSend;
            clientApi.ResponseDataReceived += ClientApi_ResponseDataReceived;
            debugLog("", "SETUP START");
        }
        private void ClientApi_ResponseDataReceived(object sender, ClientInfo.ResponseReceived e)
        {
            debugLog("<<", $"[{e.StatusCode}] RECV: {e.Content}");
        }
        private void ClientApi_BeforeSend(object sender, HttpRequestMessage e)
        {
            string content = "";
            if (e.Content != null)
            {
                if (e.Content is StringContent strCnt)
                {
                    content = strCnt.ReadAsStringAsync().Result;
                }
            }

            debugLog(">>", $"[{e.Method}] {e.RequestUri} {content}");
        }
        private void debugLog(string direciton, string content)
        {
            File.AppendAllText("debug.log", $"{DateTime.Now:G} {direciton} {content}\r\n");
        }
#endif

        protected override void atualizaClients(TokenResponse token)
        {
            clientApi.SetAuthorizationBearer(token.access_token);
        }

        /* COB */

        /// <summary>
        /// Endpoint que cria uma cobrança imediata (QRCode Pix).
        /// </summary>
        /// <param name="transactionId">String, deve ter de 27 a 36 caracteres. Identificador único da cobrança Pix</param>
        /// <param name="cobranca">Dados para geração da cobrança imediata.</param>
        /// <returns>Cobrança imediata criada</returns>
        public async Task<Models.Cobranca.CriarCobrancaResponse> CriarCobrancaAsync(string transactionId, Models.Cobranca.CriarCobrancaRequest cobranca)
        {
            validaTxID(transactionId);
            return await ExecutaChamadaAsync(() => clientApi.PutAsync<Models.Cobranca.CriarCobrancaResponse>($"/pix/api/v2/cob/{transactionId}", cobranca));
        }

        /// <summary>
        /// Endpoint para criar uma cobrança imediata, neste caso, o txid deve ser definido pelo PSP.
        /// </summary>
        /// <param name="cobranca">Dados para geração da cobrança imediata.</param>
        /// <returns>Cobrança imediata criada</returns>
        public async Task<Models.Cobranca.CriarCobrancaResponse> CriarCobrancaAsync(Models.Cobranca.CriarCobrancaRequest cobranca)
            => await ExecutaChamadaAsync(() => clientApi.PostAsync<Models.Cobranca.CriarCobrancaResponse>($"/pix/api/v2/cob", cobranca));
        /// <summary>
        /// Endpoint para revisar uma cobrança através de um determinado txid.
        /// </summary>
        /// <param name="transactionId">String, deve ter de 27 a 36 caracteres. Identificador único da cobrança Pix.</param>
        /// <param name="cobranca">Dados para geração da cobrança</param>
        /// <returns>Cobrança imediata revisada. A revisão deve ser incrementada em 1.</returns>
        public async Task<Models.Cobranca.RevisarCobrancaResponse> RevisarCobrancaAsync(string transactionId, Models.Cobranca.RevisarCobrancaRequest cobranca)
        {
            validaTxID(transactionId);
            return await ExecutaChamadaAsync(() => clientApi.PatchAsync<Models.Cobranca.RevisarCobrancaResponse>($"/pix/api/v2/cob/{transactionId}", cobranca));
        }

        /// <summary>
        /// Endpoint para consultar uma cobrança através de um determinado txid.
        /// </summary>
        /// <param name="transactionId">String, deve ter de 27 a 36 caracteres. Identificador único da cobrança Pix.</param>
        /// <param name="revisao">Revisao a ser consultada</param>
        /// <returns>Dados da cobrança imediata</returns>
        public async Task<Models.Cobranca.CobrancaCompleta> ConsultarCobrancaAsync(string transactionId, int? revisao = null)
        {
            validaTxID(transactionId);

            string url = $"/pix/api/v2/cob/{transactionId}";
            if (revisao.HasValue) url += $"?revisao={revisao.Value}";

            return await ExecutaChamadaAsync(() => clientApi.GetAsync<Models.Cobranca.CobrancaCompleta>(url));
        }
        /// <summary>
        /// Endpoint para consultar cobranças imediatas através de parâmetros como início, fim, cpf, cnpj e status.
        /// </summary>
        /// <param name="consulta">Dados da consulta</param>
        /// <returns>Lista de cobranças imediatas.</returns>
        public async Task<Models.Cobranca.ConsultaResponse> ListarCobrancasAsync(Models.Cobranca.ConsultaRequest consulta)
            => await ExecutaChamadaAsync(() => clientApi.GetAsync<Models.Cobranca.ConsultaResponse>("/pix/api/v2/cob", consulta.ToKVP()));

        /// <summary>
        /// Endpoint para gerar a imagem qrcode de uma cobrança através de um determinado txid.
        /// </summary>
        /// <param name="transactionId">String, deve ter de 27 a 36 caracteres. Identificador único da cobrança Pix.</param>
        /// <param name="revisao">Revisao a ser consultada</param>
        /// <param name="largura">Largura da imagem a ser exibida</param>
        /// <returns>Bytes da imagem codificada em PNG</returns>
        [Obsolete("Parece não estar mais disponível")]
        public async Task<byte[]> ConsultarImagemCobrancaAsync(string transactionId, int? revisao = null, int? largura = null)
        {
            validaTxID(transactionId);
            string url = $"/pix/api/v2/cob/{transactionId}/imagem";

            return await ExecutaChamadaAsync(() => clientApi.GetAsync<byte[]>(url, new { revisao, largura }.ToKVP()));
        }

        /* COBV */

        /* COBV-Lote */

        /* PIX */

        /// <summary>
        /// Endpoint para consultar Pix recebidos
        /// </summary>
        /// <param name="consulta">Dados da consulta</param>
        /// <returns>Lista dos Pix recebidos de acordo com o critério de busca.</returns>
        public async Task<Models.Pix.ConsultaResponse> ListarPIXAsync(Models.Pix.ConsultaRequest consulta)
            => await ExecutaChamadaAsync(() => clientApi.GetAsync<Models.Pix.ConsultaResponse>("/pix/api/v2/pix", consulta.ToKVP()));
        /// <summary>
        /// Endpoint para consultar um Pix através de um e2eid.
        /// </summary>
        /// <param name="endToEndId">Id fim a fim da transação. Deve ter 32 caracteres.</param>
        /// <returns>Dados do Pix efetuado.</returns>
        public async Task<Models.Pix.PixResponse> ConsultarPIXAsync(string endToEndId)
             => await ExecutaChamadaAsync(() => clientApi.GetAsync<Models.Pix.PixResponse>($"/pix/api/v2/pix/{endToEndId}"));

        /* Webhook */
        /// <summary>
        /// Endpoint para configuração do serviço de notificações acerca de Pix recebidos. 
        /// Somente Pix associados a um txid serão notificados.
        /// </summary>
        /// <param name="chave">Chave a ser associada</param>
        /// <param name="url">Url a ser chamada com POST. Será concatenado `/pix` ao final.</param>
        public async Task CriarWebHookAsync(string chave, string url)
        {
            await ExecutaChamadaAsync(() => clientApi.PutAsync($"/pix/api/v2/webhook/{chave}", new { webhookUrl = url }));
        }
        /// <summary>
        /// Endpoint para consultar Webhooks cadastrados
        /// </summary>
        public async Task<WebhookListResponse> ConsultarWebHooksAsync()
            => await ExecutaChamadaAsync(() => clientApi.GetAsync<WebhookListResponse>("/pix/api/v2/webhook"));
        /// <summary>
        /// Endpoint para recuperação de informações sobre o Webhook Pix.
        /// </summary>
        public async Task<WebhookResponse> ConsultarWebHookAsync(string chave)
            => await ExecutaChamadaAsync(() => clientApi.GetAsync<WebhookResponse>($"/pix/api/v2/webhook/{chave}"));
        /// <summary>
        /// Endpoint para cancelamento do webhook. Não é a única forma pela qual um webhook pode ser removido.
        /// </summary>
        public async Task CancelarWebHookAsync(string chave)
            => await ExecutaChamadaAsync(() => clientApi.DeleteAsync($"/pix/api/v2/webhook/{chave}"));

        private static void validaTxID(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException($"'{nameof(transactionId)}' cannot be null or empty.", nameof(transactionId));
            }

            // Mitiga ataque de negação de serviço no regex (processamento muito longo)
            if (transactionId.Length > 100)
            {
                throw new ArgumentException($"'{nameof(transactionId)}' comprimento inválido.", nameof(transactionId));
            }
            // CHeca Regex
            if (!rxTxid.IsMatch(transactionId))
            {
                throw new ArgumentException($"'{nameof(transactionId)}' Não é valido na restrição \"{rxTxidPattern}\"", nameof(transactionId));
            }

        }

    }
}
