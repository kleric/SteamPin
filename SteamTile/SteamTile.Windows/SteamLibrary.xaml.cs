using SteamTile.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace SteamTile
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class SteamLibrary : Page
    {
        private List<Game> library = new List<Game>();
        private string launched_appid;

        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        /// <summary>
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// NavigationHelper is used on each page to aid in navigation and 
        /// process lifetime management
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        public SteamLibrary()
        {
            this.InitializeComponent();
            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += navigationHelper_LoadState;

            Loaded += (s, e) =>
            {
                if (!String.IsNullOrEmpty(launched_appid))
                {
                    StartGame(launched_appid);
                }
            };
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="Common.NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void navigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            // TODO: Assign a bindable collection of items to this.DefaultViewModel["Items"]
        }

        #region NavigationHelper registration

        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// 
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="Common.NavigationHelper.LoadState"/>
        /// and <see cref="Common.NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// 

        private async void DownloadImages()
        {
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            using (var client = new HttpClient())
            {
                //client.BaseAddress = new Uri("http://cdn.akamai.steamstatic.com/");

                foreach (Game g in library)
                {
                    if ((await localFolder.GetFileAsync(g.appid + ".jpg")).IsAvailable) continue;
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, g.imgurl);

                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    var imageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(g.appid + ".jpg", CreationCollisionOption.FailIfExists);
                    var fs = await imageFile.OpenAsync(FileAccessMode.ReadWrite);
                    DataWriter writer = new DataWriter(fs.GetOutputStreamAt(0));
                    writer.WriteBytes(await response.Content.ReadAsByteArrayAsync());
                    await writer.StoreAsync();
                    writer.DetachStream();
                    await fs.FlushAsync();
                }
            }
        }

        private async void LoadLibrary()
        {

            List<Game> tempLibrary = new List<Game>();

            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            string steam_id = (string) localSettings.Values["steamid"];

            string api_key = (string)App.Current.Resources["api_key"];

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://api.steampowered.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response = await client.GetAsync("IPlayerService/GetOwnedGames/v0001/?key=" + api_key + "&steamid=" + steam_id + "&format=json&include_appinfo=1");

                if(response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    JsonValue jsonValue = JsonValue.Parse(result);

                    JsonArray games = jsonValue.GetObject().GetNamedObject("response").GetNamedArray("games");

                    int num_games = games.GetArray().Count;

                    for(uint i = 0; i < num_games; i++)
                    {
                        JsonObject game = games.GetObjectAt(i);

                        string game_name = game.GetNamedString("name");
                        long game_id = (long) game.GetNamedNumber("appid");

                        Game g = new Game()
                        {
                            name = game_name,
                            appid = game_id,
                            imgurl = "http://cdn.akamai.steamstatic.com/steam/apps/" + game_id + "/header.jpg"
                        };

                        tempLibrary.Add(g);
                    }

                    library = tempLibrary;
                }
            }
            gameGridView.ItemsSource = library;

            DownloadImages();
        }

        private async void StartGame(string appid)
        {
            var uri = new Uri(@"steam://rungameid/" + appid);

            var success = await Launcher.LaunchUriAsync(uri);

            if (!success)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                StartGame(appid);
            }
            else
            {
                Application.Current.Exit();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            navigationHelper.OnNavigatedTo(e);

            LoadLibrary();

            if(e.Parameter != null)
            {
                launched_appid = e.Parameter.ToString();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        public async void gameGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Game selectedGame = e.ClickedItem as Game;

            Uri square150x150Logo = new Uri("ms-appdata:///local/" + selectedGame.appid + ".jpg");
            TileSize newTileDesiredSize = TileSize.Wide310x150;

            SecondaryTile t2 = new SecondaryTile("steamlauncher_" + selectedGame.appid, selectedGame.name, "" + selectedGame.appid, square150x150Logo, newTileDesiredSize);
            t2.VisualElements.Wide310x150Logo = square150x150Logo;

            await t2.RequestCreateAsync();
        }
    }
}
