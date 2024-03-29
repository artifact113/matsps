﻿
namespace GMap.NET.MapProviders
{
   using System;

   /// <summary>
   /// BingSatelliteMapProvider provider
   /// </summary>
   public class BingSatelliteMapProvider : BingMapProviderBase
   {
      public static readonly BingSatelliteMapProvider Instance;

      BingSatelliteMapProvider()
      {
         MaxZoom = 19; 
      }

      static BingSatelliteMapProvider()
      {
         Instance = new BingSatelliteMapProvider();
      }

      #region GMapProvider Members

      readonly Guid id = new Guid("3AC742DD-966B-4CFB-B67D-33E7F82F2D37");
      public override Guid Id
      {
         get
         {
            return id;
         }
      }

      readonly string name = "BingSatelliteMap";
      public override string Name
      {
         get
         {
            return name;
         }
      }

      public override PureImage GetTileImage(GPoint pos, int zoom)
      {
         string url = MakeTileImageUrl(pos, zoom, LanguageStr);

         return GetTileImageUsingHttp(url);
      }

      #endregion

      string MakeTileImageUrl(GPoint pos, int zoom, string language)
      {
         string key = TileXYToQuadKey(pos.X, pos.Y, zoom);
         return string.Format(UrlFormat, GetServerNum(pos, 4), key, Version, language, (!string.IsNullOrEmpty(ClientKey) ? "&key=" + ClientKey : string.Empty));
      }

      static readonly string UrlFormat = "http://ecn.t{0}.tiles.virtualearth.net/tiles/a{1}.jpeg?g={2}&mkt={3}{4}";
   }
}
