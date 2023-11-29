using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatCliente
{
    public partial class Form1 : Form
    {
        // Tratar o nome do usuário
        private string NomeUsuario;
        private StreamWriter stwEnviador;
        private StreamReader strReceptor;
        private TcpClient tcpServidor;
        // Necessário para atualizar o formulário com mensagens da outra thread
        private delegate void AtualizaLogCallBack(string strMensagem);
        // Necessário para definir o formulário para o estado "disconnected" de outra thread
        private delegate void FechaConexaoCallBack(string strMotivo);
        private Thread mensagemThread;
        private IPAddress enderecoIP;
        private int portaHost;
        private bool Conectado;

        public Form1()
        {
            // Na saída da aplicação: desconectar
            Application.ApplicationExit += new EventHandler(OnApplicationExit);
            InitializeComponent();

        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            // se não está conectando, aguarda a conexão
            if (!Conectado)
            {
                // Inicializa a conexão
                InicializaConexao();
            }
            else
            {
                // Se está conectado, então desconecta
                FechaConexão("Desconectado a pedido do usuário.");
            }
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            EnviaMensagem();
        }

        private void txtMensagem_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Se pressionou a tecla Enter
            if (e.KeyChar == (char)13)
            {
                EnviaMensagem();
            }
        }

        private void InicializaConexao()
        {
            try
            {
                // Trata o endereço IP informado em um objeto IPAddress
                enderecoIP = IPAddress.Parse(txtServidorIP.Text);
                // Trata o número da porta do host
                portaHost = (int)numPortaHost.Value;
                // Inicia uma nova conexão TCP com o servidor chat
                tcpServidor = new TcpClient();
                tcpServidor.Connect(enderecoIP, portaHost);

                // Informa se está conectado ou não
                Conectado = true;

                // Prepara o formulário
                NomeUsuario = txtUsuario.Text;

                // Desabilita e habilita os campos apropriados
                txtServidorIP.Enabled = false;
                numPortaHost.Enabled = false;
                txtUsuario.Enabled = false;
                txtMensagem.Enabled = true;
                btnEnviar.Enabled = true;
                btnConectar.ForeColor = Color.Red;
                btnConectar.Text = "Desconectar";

                //Envia o nome do usuário ao servidor
                stwEnviador = new StreamWriter(tcpServidor.GetStream());
                stwEnviador.WriteLine(txtUsuario.Text);
                stwEnviador.Flush();

                // Inicia a thread para receber mensagens e nova comunicação
                mensagemThread = new Thread(new ThreadStart(RecebeMensagens));
                mensagemThread.IsBackground = true;
                mensagemThread.Start();

                labelStatus.Invoke(new Action(() => // o invoke executa uma determinada função em uma Thread já em uso (geralmente GUI do Windows Forms)
                {
                    labelStatus.ForeColor = Color.Green;
                    labelStatus.Text = $"Conectado ao Servidor de Chat {enderecoIP}:{portaHost}";
                }));

            }
            catch (Exception ex)
            {
                labelStatus.Invoke(new Action(() =>
                {
                    labelStatus.ForeColor = Color.Red;
                    labelStatus.Text = "Erro na conexão com servidor: \n" + ex.Message;
                }));
            }
        }

        private void RecebeMensagens()
        {
            // recebe a resposta do servidor
            strReceptor = new StreamReader(tcpServidor.GetStream());
            string ConResposta = strReceptor.ReadLine();
            // Se o primeiro caractere da resposta é 1, a conexão foi feita com sucesso
            if (ConResposta[0] == '1')
            {
                // Atualiza o formúlário para informar que está conectado
                this.Invoke(new AtualizaLogCallBack(this.AtualizaLog), new object[] { "Conectado com sucesso!" }); // o invoke precisa de objeto como segundo parâmetro. Então para enviar string, tem que ser feito dessa forma
            }
            else
            {
                // Se o primeiro caractere não for 1, a conexão falhou
                string Motivo = "Não conectado: ";
                // Extra o motivo da msg resposta. O motivo começa no 3º caractere
                Motivo += ConResposta.Substring(2, ConResposta.Length - 2);
                // Atualiza o formulário com o motivo da falha na conexão
                this.Invoke(new FechaConexaoCallBack(this.FechaConexão), new object[] { Motivo });
                // Sai do método
                return;
            }

            //Enquanto estiver conectado, lê as linhas que estão chegando do servidor
            while (Conectado)
            {
                // exibe as msgs no TextBox
                this.Invoke(new AtualizaLogCallBack(this.AtualizaLog), new object[] { strReceptor.ReadLine() });
            }
        }

        private void AtualizaLog(string strMensagem)
        {
            // Anexa texto ao final de cada linha
            txtLog.AppendText(strMensagem + "\r\n"); // "\r\n" quebra a linha e leva o cursor ao início da próxima
        }

        private void EnviaMensagem()
        {
            // Envia a mensagem para o servidor
            if (txtMensagem.Lines.Length >= 1) // verifica se tem mensagens com base na qtd de linhas
            {
                stwEnviador.WriteLine(txtMensagem.Text);
                stwEnviador.Flush();
                txtMensagem.Lines = null; // limpa a linha to txtMensagem
            }
            txtMensagem.Text = ""; // limpa o campo
        }

        private void FechaConexão(string Motivo)
        {
            // Fecha a conexão com o servidor
            // Mostra o motivo por que a conexão encerrou
            txtLog.AppendText(Motivo + "\r\n");
            // Habilita e desabilita os controles apropriados no formulário
            txtServidorIP.Enabled = true;
            numPortaHost.Enabled = true;
            txtUsuario.Enabled = true;
            txtMensagem.Enabled = false;
            btnEnviar.Enabled = false;
            btnConectar.ForeColor = Color.Green;
            btnConectar.Text = "Conectar";

            // Fecha os objetos
            Conectado = false;
            stwEnviador.Close();
            strReceptor.Close();
            tcpServidor.Close();

            labelStatus.Invoke(new Action(() =>
            {
                labelStatus.ForeColor = Color.Green;
                labelStatus.Text = $"Desconectado do Servidor de Chat {enderecoIP}:{portaHost}";
            }));
        }

        // O tratador de evento para a saída da aplicação
        public void OnApplicationExit(object sender, EventArgs e)
        {
            if(Conectado)
            {
                // Fecha as conexões, streams, etc...
                Conectado = false;
                stwEnviador.Close();
                strReceptor.Close();
                tcpServidor.Close();

                labelStatus.Invoke(new Action(() =>
                {
                    labelStatus.ForeColor = Color.Green;
                    labelStatus.Text = $"Desconectado do Servidor de Chat {enderecoIP}:{portaHost}";
                }));

            }
        }
    }
}
