using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Exceptions;
using SpotifyAPI.Web;
using TGbot.Constant;
using System.Net.Http;
using Newtonsoft.Json;

namespace TGbot
{
    public class TGbot1
    {
        TelegramBotClient botClient = new TelegramBotClient("5436829299:AAFJiiD6qhXGErl8a-IVYEJnGcsUmL2gwD0");
        CancellationToken cancellationToken = new CancellationToken();
        ReceiverOptions receiverOptions = new ReceiverOptions { AllowedUpdates = { } };

        public async Task Start()
        {
            botClient.StartReceiving(HandlerUpdateAsync, HandlerError, receiverOptions, cancellationToken);
            var mybot = await botClient.GetMeAsync();
            Console.WriteLine($"Бот {mybot.Username} почав працювати.");
            Console.ReadKey();
        }

        private Task HandlerError(ITelegramBotClient botclient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Помилка в телеграм бот АПІ:\n{apiRequestException.ErrorCode}" +
                $"\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private async Task HandlerUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update?.Message?.Text != null)
                await HandlerMessageAsync(botClient, update.Message);
        }
        string playlistsearch;
        string searchsong;
        string ames = "";
        string bmes = "";
        string urID = "";
        string PlID = "";
        string plName;
        public Dictionary<int, string> songid = new Dictionary<int, string>();
        public Dictionary<int, string> songnames = new Dictionary<int, string>();
        public Dictionary<int, string> likedsongs = new Dictionary<int, string>();
        HttpClient client = new HttpClient();

        public async Task<Dictionary<int, string>> GetPlaylistTracksID(string PlaylistID)
        {
            var request = $"{constants.address}GetPlaylistTracksID?PlaylistID={PlaylistID}";
            var httpResponse = await client.GetAsync(request);
            var content = await httpResponse.Content.ReadAsStringAsync();
            var s_id = JsonConvert.DeserializeObject<Dictionary<int, string>>(content);
            return s_id;
        }

        public async Task<Dictionary<int, string>> GetPlaylistTracksNames(string PlaylistID)
        {
            var request = $"{constants.address}GetPlaylistTracksNames?PlaylistID={PlaylistID}";
            var httpResponse = await client.GetAsync(request);
            var content = await httpResponse.Content.ReadAsStringAsync();
            var s_names = JsonConvert.DeserializeObject<Dictionary<int, string>>(content);
            return s_names;
        }
        public async Task<string> CreatePlaylist(string userID, string Name)
        {
            Uri u = new Uri($"{constants.address}CreatePlaylist?userID={userID}&Name={Name}");
            HttpContent c = new StringContent("", Encoding.UTF8, "application/json");
            var result = client.PostAsync(u, c).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            return resultContent;
        }
        public async Task<string> AddtoPlaylist(string PlaylistID, string TracksID)
        {
            Uri u = new Uri($"{constants.address}AddtoPlaylist?PlaylistID={PlaylistID}&TracksID={TracksID}");
            HttpContent c = new StringContent("", Encoding.UTF8, "application/json");
            var result = client.PutAsync(u, c).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            return resultContent;
        }
        string DisplayPlaylist()
        {
            string botmes = "Playlist:\n";
            string like = "";
            foreach (var name in songnames)
            {
                if (likedsongs.ContainsKey(name.Key)) { like = "❤"; }
                else like = "      ";
                botmes = botmes + $"{like} {name.Key} - {name.Value}\n";
            }
            if (botmes.Length < 11) { return "There is no playlist uploaded.\nUse /getplaylist to upload."; }
            return botmes;
        }
        string LikedIDtoString()
        {
            string IDs = "";
            foreach (var a in likedsongs)
                IDs = IDs + "$$" + a.Value;
            IDs = IDs.Substring(2);
            return IDs;
        }
        private async Task HandlerMessageAsync(ITelegramBotClient botClient, Message message)
        {
            //Start
            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Hi there! ✋ I am Spotify bot! 🎧 \nUsing me You can: \n- Upload playlist, \n- Like its songs, \n- Dislike them to remove likes, \n- Create new playlist consisting liked songs, \n- Add these these songs to existing playlist.\n---------------\nIMPORTANTLY! You need to get token to work with me.\nClick /settoken to see more.\nPress /help to see all commands, and /keyboard to unlock buttons.");
                return;
            }

            //Getting help and list of commands
            if (message.Text == "/help" || message.Text == "help")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "📃List of possible commands:\n/help - To get help with commands and their usage.\n/keyboard - To get quick access to /display and /help commands.\n" +
                        "/settoken - To set generated spotify Token, which will allow you to work with bot.(Token is working 1 hour after generating)\n/getplaylist - To upload playlist from Spotify for future displaying and liking songs.\n/searchsong - To search song by its name.\n/like - To like songs from displayed playlist.\n" +
                        "/dislike - To remove likes from liked songs.\n/createlikedplaylist - To create new playlist which will consist liked songs from displayed playlist.\n" +
                        "/addlikedtoplaylist - To add liked songs from displayed playlist to already existing playlist.\n/display - Another way to display uploaded playlist.\nAll of them u can use after typing '/', or clicking Menu button at the bottom of the screen.😄");
                return;
            }

            //Enabling Keyboard
            if (message.Text == "/keyboard")
            {
                ReplyKeyboardMarkup replyKeyboardMarkup = new
                    (
                        new[] {
                            new KeyboardButton [] { "display", "help" }
                        }
                    )
                {
                    ResizeKeyboard = true
                };
                await botClient.SendTextMessageAsync(message.Chat.Id, "Here are few useful commands:", replyMarkup: replyKeyboardMarkup);
                return;
            }

            //Setting spotify token
            if (message.Text == "/settoken")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "IMPORTANTLY! You need to get a token linked to your spotify account, otherwise I won't be able to work properly🙁\nTo get it, click this link https://developer.spotify.com/console/post-playlists/ \nPress green button 'Get Token'.\nYou will see menu, where u NEED TO TICK 2 reccomended scopes: 'platlist-modify-public' and 'playlist-modify-private'.\nNow, you will be asked if this is you.\nAgree, but be attentive, you need to get token with same spotify account you are using.\n---------------\nGenerated token will be written to the left of the button 'Get Token'. \nEnter after this message:");
                ames = message.Text;
                return;
            }
            if (message.Text != "/settoken" && ames == "/settoken")
            {
                constants.token = message.Text;
                await botClient.SendTextMessageAsync(message.Chat.Id, "Token was set. If after this commands won't work, maybe you missed few symbols.");
                ames = "";
                return;
            }
            //Uploading playlist to Bot
            if (message.Text == "/getplaylist")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter playlist ID or URL:");
                ames = message.Text;
                return;
            }
            if (message.Text != "/getplaylist" && ames == "/getplaylist")
            {
                playlistsearch = message.Text;
                likedsongs.Clear();
                playlistsearch = playlistsearch.Replace(" ", "");

                //string first4 = playlistsearch.Substring(0, 4);
                if (playlistsearch.Substring(0, 4).Equals("http"))
                    playlistsearch = playlistsearch.Substring(34, 22);
                try
                {
                    songid = await GetPlaylistTracksID(playlistsearch);
                    songnames = await GetPlaylistTracksNames(playlistsearch);
                }
                catch (Exception)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Entered URL or ID isn't existing, or it was typed wrong.");
                    return;
                }
                await botClient.SendTextMessageAsync(message.Chat.Id, "Uploaded. To see the playlist type /display or press 'display' button at the bottom.");
                ames = "";
                return;
            }

            //Searching song by its name
            if (message.Text == "/searchsong")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter song name:");
                ames = message.Text;
                return;
            }
            if (message.Text != "/searchsong" && ames == "/searchsong")
            {
                searchsong = message.Text;
                var spotify = new SpotifyClient(constants.token);
                var search = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Track, searchsong));
                int i = 1;
                await foreach (var item in spotify.Paginate(search.Tracks, (s) => s.Tracks))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, $"URL - https://open.spotify.com/track/{item.Id}\nID - {item.Id}");
                    if (i == constants.limit) break;
                    i++;
                }
                ames = "";
                return;
            }

            //Liking tracks
            if (message.Text == "/like")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter the song numbers You want to like:");
                ames = message.Text;
                return;
            }
            if (message.Text != "/like" && ames == "/like")
            {
                string liked = message.Text.Replace(" ", "");
                if (liked.StartsWith(","))
                {
                    liked = liked.Remove(0, 1);
                    if (liked.EndsWith(","))
                        liked = liked.Remove(liked.Length - 1);
                }
                string[] numbers = liked.Split(',');
                try
                {
                    foreach (var n in numbers)
                    {
                        try { likedsongs[int.Parse(n)] = songid[int.Parse(n)]; }
                        catch (Exception) { }
                    }
                }
                catch (Exception)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Entered numbers aren't valid.");
                    return;
                }
                await botClient.SendTextMessageAsync(message.Chat.Id, "Liked. To see the playlist type /display or press 'display' button at the bottom.");
                ames = "";
                return;
            }

            //Disliking liked tracks 
            if (message.Text == "/dislike")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter the song numbers You want to unmark likes:");
                ames = message.Text;
                return;
            }
            if (message.Text != "/dislike" && ames == "/dislike")
            {
                string disliked = message.Text.Replace(" ", "");
                if (disliked.StartsWith(","))
                {
                    disliked = disliked.Remove(0, 1);
                    if (disliked.EndsWith(","))
                        disliked = disliked.Remove(disliked.Length - 1);
                }
                string[] numbers = disliked.Split(',');
                await botClient.SendTextMessageAsync(message.Chat.Id, "Disliked. To see the playlist type /display or press 'display' button at the bottom.");
                try
                {
                    foreach (var n in numbers)
                    {
                        if (likedsongs.ContainsKey(int.Parse(n)))
                        {
                            likedsongs.Remove(int.Parse(n));
                            /*try {  }
                            catch (Exception) { await botClient.SendTextMessageAsync(message.Chat.Id, "Aha"); }*/
                        }
                    }
                }
                catch
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Error with song numbers.");
                    ames = " ";
                    return;
                }
                ames = "";
                return;
            }

            //Creating new playlist
            if (message.Text == "/createlikedplaylist")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter name of the playlist:");
                ames = message.Text;
                return;
            }
            if (message.Text != "/createlikedplaylist" && ames == "/createlikedplaylist" && bmes == "")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter your user ID or URL of your spotify profile:");
                plName = message.Text;
                bmes = "named";
                urID = bmes.Replace(" ", "");

                string first4 = urID.Substring(0, 4);
                if (first4.Equals("http"))
                    urID = urID.Substring(30, 22);
                else if (playlistsearch.Length != 22)
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Wrong URL or ID");
                return;
            }
            if (message.Text != "/createlikedplaylist" && ames == "/createlikedplaylist" && bmes == "named")
            {
                string usID = message.Text.Replace(" ", "");
                try
                {
                    PlID = await CreatePlaylist(usID, plName);
                    await AddtoPlaylist(PlID, LikedIDtoString());
                }
                catch (Exception)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Entered user's URL or ID isn't valid.");
                    ames = "";
                    bmes = "";
                    return;
                }

                await botClient.SendTextMessageAsync(message.Chat.Id, "Created. Playlist ID - " + PlID);
                ames = "";
                bmes = "";
                return;
            }

            //Adding to existing playlist
            if (message.Text == "/addlikedtoplaylist")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Enter ID of existing playlist, where You can add songs:");
                ames = message.Text;
                return;
            }
            if (message.Text != "/addlikedtoplaylist" && ames == "/addlikedtoplaylist")
            {
                string PlaylistID = message.Text.Replace(" ", "");
                if (PlaylistID.Substring(0, 4).Equals("http"))
                    PlaylistID = PlaylistID.Substring(34, 22);
                try { await AddtoPlaylist(PlaylistID, LikedIDtoString()); }
                catch (Exception)
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Something went wrong.");
                    ames = "";
                }

                await botClient.SendTextMessageAsync(message.Chat.Id, "Added.");
                ames = "";
                return;
            }

            //Displaying playlist
            if (message.Text == "/display" || message.Text == "display")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, DisplayPlaylist());
                return;
            }

            //Unrecongized command
            else
                await botClient.SendTextMessageAsync(message.Chat.Id, "Unexpected message.😶");
        }
    }
}