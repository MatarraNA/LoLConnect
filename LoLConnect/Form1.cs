using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LCUSharp;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Reflection;

namespace LoLConnect
{
    public partial class Form1 : Form
    {
        // create api
        LeagueClientApi api = null;
        string lobbyPassword = "hard password";
        string summonerName = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            api = await LCUSharp.LeagueClientApi.ConnectAsync();
            await api.RiotClientEndpoint.ShowUxAsync();

            // create game
            await CreateCustomGame();

            // after game creation, obtain lobby id
            await Task.Delay(1000);
            var lobby = await GetCustomLobby();
            textBox1.Text = lobby.id.ToString();
        }

        public async Task<CustomGameListRoot> GetCustomLobby()
        {
            // join custom game by id
            string url = $"lol-lobby/v1/custom-games";
            var queryParameters = Enumerable.Empty<string>();
            try
            {
                // post first
                await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Post, "lol-lobby/v1/custom-games/refresh",
                                                                         queryParameters);
                await Task.Delay(100);

                var json = await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, url,
                                                                         queryParameters);
                var output = JsonConvert.DeserializeObject<List<CustomGameListRoot>>(json);
                foreach( var lobby in output )
                {
                    if( lobby.ownerSummonerName.Equals( await GetSummonerName(), StringComparison.CurrentCultureIgnoreCase ) || lobby.lobbyName.Contains(await GetSummonerName()))
                    {
                        // this is the LOBBY
                        return lobby;
                    }
                }
            }
            catch ( Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return null;
        }

        public async Task<string> GetSummonerName()
        {
            if (summonerName != "") return summonerName;
            var queryParameters = Enumerable.Empty<string>();
            try
            {
                var json = await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, "lol-summoner/v1/current-summoner",
                                                                         queryParameters);
                var output = JsonConvert.DeserializeObject<SummonerRoot>(json);
                summonerName = output.displayName;
                return output.displayName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "null";
            }
        }

        public async Task CreateCustomGame()
        {
            // Update the current summoner's profile icon to 23.
            CreateCustomGameRoot obj = new CreateCustomGameRoot();
            obj.isCustom = true;
            CustomGameLobby lobby = new CustomGameLobby();
            obj.customGameLobby = lobby;
            // random seed
            int random = new Random().Next(100);
            lobby.lobbyName = (await GetSummonerName()) + "'s In-House Lobby " + random;
            lobby.lobbyPassword = lobbyPassword;
            Configuration config = new Configuration();
            lobby.configuration = config;
            config.gameMode = "CLASSIC";
            config.gameMutator = "GAME_CFG_DRAFT_TOURNAMENT";
            config.gameServerRegion = "";
            config.spectatorPolicy = "AllAllowed";
            config.teamSize = 5;
            config.mapId = 11;
            config.mutators = new Mutators();
            config.mutators.id = 6;
            var queryParameters = Enumerable.Empty<string>();
            try
            {
                var json = await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Post, "lol-lobby/v2/lobby",
                                                                         queryParameters, obj);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException);
                MessageBox.Show(ex.Message);
            }
        }

        public class Mutators
        {
            public int id { get; set; }
        }

        public class Configuration
        {
            public string gameMode { get; set; }
            public string gameMutator { get; set; }
            public string gameServerRegion { get; set; }
            public int mapId { get; set; }
            public Mutators mutators { get; set; }
            public string spectatorPolicy { get; set; }
            public int teamSize { get; set; }
        }

        public class CustomGameLobby
        {
            public Configuration configuration { get; set; }
            public string lobbyName { get; set; }
            public object lobbyPassword { get; set; }
        }

        public class CreateCustomGameRoot
        {
            public CustomGameLobby customGameLobby { get; set; }
            public bool isCustom { get; set; }
        }

        public class RerollPoints
        {
            public int currentPoints { get; set; }
            public int maxRolls { get; set; }
            public int numberOfRolls { get; set; }
            public int pointsCostToRoll { get; set; }
            public int pointsToReroll { get; set; }
        }

        public class SummonerRoot
        {
            public long accountId { get; set; }
            public string displayName { get; set; }
            public string internalName { get; set; }
            public int percentCompleteForNextLevel { get; set; }
            public int profileIconId { get; set; }
            public string puuid { get; set; }
            public RerollPoints rerollPoints { get; set; }
            public long summonerId { get; set; }
            public int summonerLevel { get; set; }
            public int xpSinceLastLevel { get; set; }
            public int xpUntilNextLevel { get; set; }
        }

        public class CustomGameListRoot
        {
            public int filledPlayerSlots { get; set; }
            public int filledSpectatorSlots { get; set; }
            public string gameType { get; set; }
            public bool hasPassword { get; set; }
            public object id { get; set; }
            public string lobbyName { get; set; }
            public int mapId { get; set; }
            public int maxPlayerSlots { get; set; }
            public int maxSpectatorSlots { get; set; }
            public string ownerSummonerName { get; set; }
            public string passbackUrl { get; set; }
            public string spectatorPolicy { get; set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox1.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            textBox2.Text = Clipboard.GetText();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            api = await LCUSharp.LeagueClientApi.ConnectAsync();
            await api.RiotClientEndpoint.ShowUxAsync();

            // join it!
            await JoinCustomGame();

        }

        public async Task JoinCustomGame()
        {
            // join custom game by id
            api = await LCUSharp.LeagueClientApi.ConnectAsync();
            string url = $"lol-lobby/v1/custom-games/{textBox2.Text}/join";
            var body = new { asSpectator = false, password = lobbyPassword };
            var queryParameters = Enumerable.Empty<string>();
            try
            {
                var json = await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Post, url,
                                                                         queryParameters, body);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void richTextBox1_Enter(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            api = await LCUSharp.LeagueClientApi.ConnectAsync();
            await api.RiotClientEndpoint.ShowUxAsync();

            // parse all players names from textbox
            string unparsed = richTextBox1.Text;
            string[] namesArr = unparsed.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for(int i = 0; i < namesArr.Length; i++ )
            {
                // remove white space and everything before including the period
                namesArr[i] = namesArr[i].Replace(" ", "");
                if(namesArr[i].Contains(".") )
                {
                    // remove everything before and upto dot
                    namesArr[i] = namesArr[i].Substring(namesArr[i].IndexOf('.') + 1);
                }
                // ready to go!
            }
            foreach( var playerName in namesArr )
            {
                await InvitePlayer(playerName);
            }
        }

        public async Task<SummonerRoot> GetSummonerAsync(string name )
        {
            string url = $"lol-summoner/v1/summoners";
            IEnumerable<string> queryParameters = new string[] { $"name={name}" };
            try
            {
                var json = await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Get, url,
                                                                         queryParameters);

                var summoner = JsonConvert.DeserializeObject<SummonerRoot>(json);


                // can now invite summoner
                return summoner;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public async Task InvitePlayer( string name )
        {
            // join custom game by id
            var summoner = await GetSummonerAsync(name);

            //inv that player!
            string url = $"lol-lobby/v2/lobby/invitations";
            IEnumerable<string> queryParameters = Enumerable.Empty<string>();
            var player = new List<PlayerToInvite>();
            player.Add(new PlayerToInvite());
            player.FirstOrDefault().toSummonerId = summoner.summonerId.ToString();
            player.FirstOrDefault().toSummonerName = summoner.displayName;

            try
            {
                var json = await api.RequestHandler.GetJsonResponseAsync(HttpMethod.Post, url, queryParameters, player);
            }
            catch( Exception ex )
            {
                MessageBox.Show($"Failed to invite {summoner.displayName}\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public class PlayerToInvite
        {
            public string toSummonerId;
            public string toSummonerName;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
