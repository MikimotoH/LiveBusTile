using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduledTaskAgent1
{
    /// <summary>
    /// TWD97(Zone 121) coordinate http://spatialreference.org/ref/epsg/twd97-tm2-zone-121/
    ///    which subtracts 250,000 meters. So that coordinate x of ChiaNan Plain will be negative value.
    /// </summary>
    public class TWD97Coord
    {
        public double x;
        public double y;
        public TWD97Coord(double x, double y)
        {
            this.x = x; this.y = y;
        }
        
        public double DistanceFrom2(TWD97Coord b)
        {
            return pow2(x - b.x) + pow2(y - b.y);
        }
        public override string ToString()
        {
            return x.ToString("R")+ "," + y.ToString("R");
        }
        public override bool Equals(object obj)
        {
            TWD97Coord b = (TWD97Coord)obj;
            return x == b.x && y == b.y;
        }
        public override int GetHashCode()
        {
            return x.GetHashCode()^y.GetHashCode();
        }
        public static TWD97Coord FromLatLng(GeoCoordinate g)
        {
            return FromLatLng(g.Latitude, g.Longitude);
        }

        static Func<double, double> sin = v => Math.Sin(v);
        static Func<double, double> cos = v => Math.Cos(v);
        static Func<double, double> tan = v => Math.Tan(v);
        static Func<double, double> sqrt = v => Math.Sqrt(v);
        static Func<double, double> ToRadians = v => v * (Math.PI / 180);
        static Func<double, double> pow2 = v => v * v;
        static Func<double, double> pow3 = v => v * v * v;
        static Func<double, double> pow4 = v => v * v * v * v;
        static Func<double, double> pow5 = v => v * v * v * v * v;

        /// <summary>
        /// http://blog.ez2learn.com/2009/08/15/lat-lon-to-twd97/
        ///  Convert Latitude(in radians) and  Longitude(in radians) to TWD97 (http://spatialreference.org/ref/epsg/twd97-tm2-zone-121/)
        /// </summary>
        /// <param name="latitude_in_degrees"> Latitude in degrees 緯度</param>
        /// <param name="longitude_in_degrees"> Longitude in degrees 經度</param>
        /// <returns>TWD97Coord</returns>
        public static TWD97Coord FromLatLng(double latitude_in_degrees, double longitude_in_degrees)
        {
            double lat = ToRadians(latitude_in_degrees);
            double lon = ToRadians(longitude_in_degrees);
            const double a = 6378137.0;// equatorial radius in meters
            const double b = 6356752.314245; // polar radius in meters
            const double long0 = 121*(Math.PI/180);// central meridian of eastern zone 121
            const double k0 = 0.9999; // scale factor along long0 (eastern meridian 121)
            

            double e = sqrt(1 - pow2(b) / pow2(a));
            double e2 = pow2(e) / (1d - pow2(e) );
            const double n = (a - b) / (a + b);
            double nu = a / sqrt(1 - (pow2(e)) * (pow2(sin(lat))));
            double p = lon - long0;

            double A = a * (1 - n + (5d/4d) * (pow2(n) - pow3(n) ) + (81d / 64d) * (pow4(n) - pow5(n)));
            double B = (3 * a * n / 2.0) * (1 - n + (7d/8d) * (pow2(n) - pow3(n)) + (55d / 64d) * (pow4(n) - pow5(n)));
            double C = (15 * a * (pow2(n) / 16d)) * (1 - n + (3d / 4d) * (pow2(n) - pow3(n)));
            double D = (35 * a * (pow3(n)) / 48d) * (1 - n + (11d / 16d) * (pow2(n) - pow3(n)));
            double E = (315 * a * (pow4(n)) / 51d) * (1 - n);

            double S = A * lat - B * sin(2 * lat) + C * sin(4 * lat) - D * sin(6 * lat) + E * sin(8 * lat);

            double K1 = S * k0;
            double K2 = k0 * nu * sin(2 * lat) / 4d;
            double K3 = (k0 * nu * sin(lat) * pow3(cos(lat)) / 24d ) *
                (5 - pow2(tan(lat)) + 9 * e2 * pow2(cos(lat)) + 4 * pow2(e2) * (pow4(cos(lat))));

            double y = K1 + K2 * pow2(p) + K3 * pow4(p) ;

            double K4 = k0 * nu * cos(lat);
            double K5 = (k0 * nu * pow3(cos(lat)) / 6d ) *
                (1 - pow2(tan(lat)) + e2 * pow2(cos(lat)) );

            double x = K4 * p + K5 * pow3(p);
            return new TWD97Coord(x, y);

        }
    }

}
