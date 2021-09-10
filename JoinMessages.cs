using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins {
    [Info("JoinMessages", "Glowstick", "1.0.0")]
    [Description("join/leave messages")]
    class JoinMessages : CovalencePlugin {
        private class Response {
            [JsonProperty("country")]
            public string Country { get; set; }
            [JsonProperty("countryCode")]
            public string CountryCode { get; set; }
        }

        private void OnServerInitialized() {
            permission.RegisterPermission("JoinMessages.hidden", this);
#if HURTWORLD
            GameManager.Instance.ServerConfig.ChatConnectionMessagesEnabled = false;
#endif
        }

        protected override void LoadDefaultConfig() {
            LogWarning("Creating a new configuration file");
            Config["Show Join Message"] = true;
            Config["Show Leave Message"] = true;
            Config["Show Admin Join Message"] = true;
            Config["Show Admin Leave Message"] = true;
            Config["Country Settings", "Show Country Message"] = true;
            Config["Country Settings", "Use Country Code"] = false;
#if RUST
            Config["Chat Icon", "Default (SteamID64)"] = 0;
            Config["Chat Icon", "Use Steam Avatar"] = false;
            Config["Chat Icon", "Hidden"] = false;
#endif
        }

        protected override void LoadDefaultMessages() {
            lang.RegisterMessages(new Dictionary<string, string> {
                ["Join Country Message"] = "[{1}] [#edb75f]{0} joined the game.",
                ["Join Message"] = "[#edb75f]{0} joined the game.",
                ["Leave Message"] = "[#696969]{0} left the game.",
                ["Local Network"] = "Local Network"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string> {
                ["Join Country Message"] = "[{1}] {0} dołączył do gry.",
                ["Join Message"] = "{0} dołączył do gry.",
                ["Leave Message"] = "{0} opuścił nas.",
                ["Local Network"] = "Lokalna sieć"
            }, this, "pl");
            lang.RegisterMessages(new Dictionary<string, string> {
                ["Join Country Message"] = "[{1}] {0} присоединился к игре.",
                ["Join Message"] = "{0} присоединился к игре.",
                ["Leave Message"] = "{0} покинул игру.",
                ["Local Network"] = "Локальная сеть"
            }, this, "ru");
        }
        private void OnUserConnected(IPlayer player) {
            if ((player.IsAdmin && !Convert.ToBoolean(Config["Show Admin Join Message"])) || !Convert.ToBoolean(Config["Show Join Message"]) || (!player.IsAdmin && player.HasPermission("JoinMessages.hidden"))) {
                return;
            }

            if (Convert.ToBoolean(Config["Country Settings", "Show Country Message"])) {
                string country = string.Empty;
                if (player.Address.StartsWith("10.") || player.Address.StartsWith("172.16.") || player.Address.StartsWith("192.168.") || player.Address.StartsWith("127.0.0.1")) {
                    if (Convert.ToBoolean(Config["Country Settings", "Use Country Code"])) {
                        country = "LAN";
                    } else {
                        country = lang.GetMessage("Local Network", this);
                    }
                    Broadcast("Join Country Message", player, country);
                    return;
                }

                string apiUrl = "http://ip-api.com/json/";
                webrequest.Enqueue(apiUrl + player.Address, null, (code, response) => {
                    if (code != 200 || response == null) {
                        PrintWarning($"WebRequest to {apiUrl} failed, sending connect message without the country.");
                        Broadcast("Join Message", player);
                        return;
                    }

                    if (Convert.ToBoolean(Config["Use Country Code"])) {
                        country = JsonConvert.DeserializeObject<Response>(response).CountryCode;
                    } else {
                        country = JsonConvert.DeserializeObject<Response>(response).Country;
                    }
                    Broadcast("Join Country Message", player, country);
                }, this, RequestMethod.GET);

            } else {
                Broadcast("Join Message", player);
            }
        }

        private void OnUserDisconnected(IPlayer player) {
            if ((player.IsAdmin && !Convert.ToBoolean(Config["Show Admin Leave Message"])) || !Convert.ToBoolean(Config["Show Leave Message"]) || (!player.IsAdmin && player.HasPermission("JoinMessages.hidden"))) {
                return;
            } else {
                Broadcast("Leave Message", player);
            }
        }

        private void Broadcast(string msg, object player, string country = null)
        {
            var iPlayer = player as IPlayer;
#if RUST
            if (!Convert.ToBoolean(Config["Chat Icon", "Use Steam Avatar"]) && Convert.ToBoolean(Config["Chat Icon", "Hidden"])) {
                ConsoleNetwork.BroadcastToAllClients("chat.add", 2, 1, string.Format(lang.GetMessage(msg, this), iPlayer.Name, country));
            } else if (Convert.ToBoolean(Config["Chat Icon", "Use Steam Avatar"]) && !Convert.ToBoolean(Config["Chat Icon", "Hidden"])) {
                ConsoleNetwork.BroadcastToAllClients("chat.add", 2, iPlayer.Id, string.Format(lang.GetMessage(msg, this), iPlayer.Name, country));
            } else {
                ConsoleNetwork.BroadcastToAllClients("chat.add", 2, Convert.ToUInt64(Config["Chat Icon", "Default (SteamID64)"]), string.Format(lang.GetMessage(msg, this), iPlayer.Name, country));
            }
#elif HURTWORLD // Send clear message without announcement icon
            ChatManagerServer.Instance.SendChatMessage(new ServerChatMessage(string.Format(lang.GetMessage(msg, this), iPlayer.Name, country), false));
#else
            server.Broadcast(string.Format(lang.GetMessage(msg, this), iPlayer.Name, country));
#endif
        }
    }
}