namespace XisfSharp;

public static class XisfNamespace
{
    public static class Observer
    {
        public static readonly string EmailAddress = "Observer:EmailAddress";
        public static readonly string Name = "Observer:Name";
        public static readonly string PostalAddress = "Observer:PostalAddress";
        public static readonly string Website = "Observer:Website";
    }

    public static class Organization
    {
        public static readonly string EmailAddress = "Organization:EmailAddress";
        public static readonly string Name = "Organization:Name";
        public static readonly string PostalAddress = "Organization:PostalAddress";
        public static readonly string Website = "Organization:Website";
    }

    public static class Observation
    {
        public static readonly string CelestialReferenceSystem = "Observation:CelestialReferenceSystem";
        public static readonly string BibliographicReferences = "Observation:BibliographicReferences";
        public static readonly string Description = "Observation:Description";
        public static readonly string Equinox = "Observation:Equinox";
        public static readonly string GeodeticReferenceSystem = "Observation:GeodeticReferenceSystem";
        public static readonly string RelatedResources = "Observation:RelatedResources";
        public static readonly string Title = "Observation:Title";

        public static class Center
        {
            public static readonly string Dec = "Observation:Center:Dec";
            public static readonly string RA = "Observation:Center:RA";
            public static readonly string X = "Observation:Center:X";
            public static readonly string Y = "Observation:Center:Y";
        }

        public static class Location
        {
            public static readonly string Elevation = "Observation:Location:Elevation";
            public static readonly string Latitude = "Observation:Location:Latitude";
            public static readonly string Longitude = "Observation:Location:Longitude";
            public static readonly string Name = "Observation:Location:Name";
        }

        public static class Meteorology
        {
            public static readonly string AmbientTemperature = "Observation:Meteorology:AmbientTemperature";
            public static readonly string AtmosphericPressure = "Observation:Meteorology:AtmosphericPressure";
            public static readonly string RelativeHumidity = "Observation:Meteorology:RelativeHumidity";
            public static readonly string WindDirection = "Observation:Meteorology:WindDirection";
            public static readonly string WindGust = "Observation:Meteorology:WindGust";
            public static readonly string WindSpeed = "Observation:Meteorology:WindSpeed";
        }

        public static class Object
        {
            public static readonly string Dec = "Observation:Object:Dec";
            public static readonly string Name = "Observation:Object:Name";
            public static readonly string RA = "Observation:Object:RA";
        }

        public static class Time
        {
            public static readonly string End = "Observation:Time:End";
            public static readonly string Start = "Observation:Time:Start";
        }
    }

    public static class Instrument
    {
        public static readonly string ExposureTime = "Instrument:ExposureTime";

        public static class Camera
        {
            public static readonly string Gain = "Instrument:Camera:Gain";
            public static readonly string ISOSpeed = "Instrument:Camera:ISOSpeed";
            public static readonly string Name = "Instrument:Camera:Name";
            public static readonly string ReadoutNoise = "Instrument:Camera:ReadoutNoise";
            public static readonly string Rotation = "Instrument:Camera:Rotation";
            public static readonly string XBinning = "Instrument:Camera:XBinning";
            public static readonly string YBinning = "Instrument:Camera:YBinning";
        }

        public static class Filter
        {
            public static readonly string Name = "Instrument:Filter:Name";
        }

        public static class Focuser
        {
            public static readonly string Position = "Instrument:Focuser:Position";
        }

        public static class Sensor
        {
            public static readonly string TargetTemperature = "Instrument:Sensor:TargetTemperature";
            public static readonly string Temperature = "Instrument:Sensor:Temperature";
            public static readonly string XPixelSize = "Instrument:Sensor:XPixelSize";
            public static readonly string YPixelSize = "Instrument:Sensor:YPixelSize";
        }

        public static class Telescope
        {
            public static readonly string Aperture = "Instrument:Telescope:Aperture";
            public static readonly string CollectingArea = "Instrument:Telescope:CollectingArea";
            public static readonly string FocalLength = "Instrument:Telescope:FocalLength";
            public static readonly string Name = "Instrument:Telescope:Name";
        }
    }

    public static class Image
    {
        public static readonly string FrameNumber = "Image:FrameNumber";
        public static readonly string GroupId = "Image:GroupId";
        public static readonly string SubgroupId = "Image:SubgroupId";
    }

    public static class Processing
    {
        public static readonly string Description = "Processing:Description";
        public static readonly string History = "Processing:History";
    }
}
