using System;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Locations;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System.Xml;
using Android.Util;
using System.Collections.Generic;
using Android.Media;
using Android.Views.InputMethods;
using Android.Content;

using Felipecsl.GifImageViewLibrary;
using Android.Graphics;
using static Felipecsl.GifImageViewLibrary.GifImageView;
using System.Net.Http;

namespace AlarmTrip
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {
        static EditText editCity;
        CoordinatorLayout maplayout;
        FloatingActionButton fab;
        FloatingActionButton fabmap;
        private GoogleMap mMap;
        private MarkerOptions _mrkOpt;
        static string destcity = "";
        static string actcity = "";
        readonly string tag = "AlarmTrip";
        List<Marker> markerList = new List<Marker>();
        static GifImageView gifImageView;
        Ringtone r;
        static bool stoplocation = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.LoadScreen);
            gifImageView = FindViewById<GifImageView>(Resource.Id.gifImageView);
            LoadScreen();
        }

        private void Load()
        {
            SetContentView(Resource.Layout.activity_main);

            Button button = FindViewById<Button>(Resource.Id.button1);
            editCity = FindViewById<EditText>(Resource.Id.editText1);

            button.Click += StartMap;
            maplayout = FindViewById<CoordinatorLayout>(Resource.Id.maplayout);

            var mapFragment = (MapFragment)FragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);

            fabmap = FindViewById<FloatingActionButton>(Resource.Id.fabmap);

            fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += GoToMap;

            fabmap.Click += BackToHome;
        }
        private async void LoadScreen()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var bytes = await client.GetByteArrayAsync("https://i.imgur.com/8q8Z0yM.gif");
                    gifImageView.SetBytes(bytes);
                    gifImageView.StartAnimation();
                }
                await Task.Delay(5000);
                Load();
            }
            catch (Exception ex)
            {
                Android.App.AlertDialog.Builder alert = new Android.App.AlertDialog.Builder(this);
                alert.SetCancelable(false);
                alert.SetTitle("AlarmTrip");
                alert.SetMessage("Brak połączenia z internetem!\nDo prawidłowego działania aplikacji niezbędne jest połączenie z interentem.");
                alert.SetPositiveButton("Ok", (senderAlert, args) => {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });
                Dialog dialog = alert.Create();
                dialog.Show();
                Log.Info(tag, "Error: " + ex.ToString());
            }
        }
        public override bool OnTouchEvent(MotionEvent e)
        {
            InputMethodManager inputManager = (InputMethodManager)GetSystemService(Context.InputMethodService);
            var currentFocus = Window.CurrentFocus;
            if (currentFocus != null)
            {
                inputManager.HideSoftInputFromWindow(currentFocus.WindowToken, HideSoftInputFlags.NotAlways);
            }
            return base.OnTouchEvent(e);
        }

        private void StartMap(object sender, EventArgs e)
        {
            Restart();
            if (!string.IsNullOrEmpty(editCity.Text))
            {
                RunOnUiThread(() => {
                    var coord = GetCityCoord(editCity.Text);
                    if(coord.Item1 != 0 && coord.Item2 != 0)
                    {
                        LoadMarkers(coord.Item1, coord.Item2);
                        fab.Visibility = ViewStates.Invisible;
                        destcity = editCity.Text;
                        maplayout.Visibility = ViewStates.Visible;
                        LatLng latlng = new LatLng(coord.Item1, coord.Item2);
                        mMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(latlng,10));
                    }
                    else
                    {
                        Toast.MakeText(this, "Zła Miejscowość!", ToastLength.Long).Show();
                        editCity.Text = "";
                    }
                });
            }
        }
        public void GetCityName(string coordinate)
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load("https://geocode.xyz/" + coordinate + "?geoit=xml");

                XmlNodeList xNodelst = xDoc.GetElementsByTagName("geodata");
                XmlNode xNode = xNodelst.Item(0);
                string City = xNode.SelectSingleNode("city").InnerText;
                actcity = City;
            }
            catch(Exception ex)
            {
                Log.Info(tag, "Error: " + ex.ToString());
            }

        }
    
        (double, double) GetCityCoord(string name)
        {
            try
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load("https://geocode.xyz/" + name + "?geoit=xml");

                XmlNodeList xNodelst = xDoc.GetElementsByTagName("geodata");
                XmlNode xNode = xNodelst.Item(0);
                string latt = xNode.SelectSingleNode("latt").InnerText;
                string longt = xNode.SelectSingleNode("longt").InnerText;
                return (Convert.ToDouble(latt), Convert.ToDouble(longt));
            }
            catch (Exception ex)
            {
                Log.Info(tag, "Error: " + ex.ToString());
                return (0,0);
            }

        }
        public async void LoadMarkers(double doubleLat, double doubleLong)
        {
            if (mMap == null) return;
            for (var i = 1; i < 500; i++)
            {
                _mrkOpt = new MarkerOptions();
                _mrkOpt.SetPosition(new LatLng(doubleLat, doubleLong));
                Marker Mar = mMap.AddMarker(_mrkOpt);
                markerList.Add(Mar);
                await Task.Delay(5);
            }
        }


        private void BackToHome(object sender, EventArgs e)
        {
            fab.Visibility = ViewStates.Visible;
            maplayout.Visibility = ViewStates.Invisible;
        }

        private void Restart()
        {
            fab.Visibility = ViewStates.Invisible;
            maplayout.Visibility = ViewStates.Invisible;
            destcity = "";
            foreach (Marker m in markerList)
            {
                m.Remove();
            }
            stoplocation = false;
        }

        public void OnMapReady(GoogleMap map)
        {
            mMap = map;
            mMap.MyLocationEnabled = true;
            if (!stoplocation)
            {
                map.MyLocationChange += (_, e) => {

                    Task.Factory.StartNew(() => {
                        var latitude = e.Location.Latitude;
                        var longitude = e.Location.Longitude;
                        string coordinates = latitude.ToString() + "," + longitude.ToString();
                        if (!string.IsNullOrEmpty(coordinates))
                        {
                            GetCityName(coordinates);
                        }
                    }).ContinueWith(task =>
                    {
                        if (actcity.ToLower() == destcity.ToLower())
                        {
                            stoplocation = true;
                            Android.Net.Uri notification = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);
                            r = RingtoneManager.GetRingtone(Application.Context, notification);
                            r.Play();
                            Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
                            alert.SetCancelable(false);
                            alert.SetTitle("AlarmTrip");
                            alert.SetMessage("Jesteś na miejscu");
                            alert.SetPositiveButton("Ok", (senderAlert, args) =>
                            {
                                r.Stop();
                                Restart();
                            });
                            Dialog dialog = alert.Create();
                            dialog.Show();
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());

                };
            }
        }

        private void GoToMap(object sender, EventArgs eventArgs)
        {
            maplayout.Visibility = ViewStates.Visible;
            fab.Visibility = ViewStates.Invisible;
        }
    }
}

