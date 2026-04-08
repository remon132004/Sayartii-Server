using System;

namespace Sayartii.Api.Models
{
    public class DataFromCar
    {
        public int Id { get; set; }
        public string User_id { get; set; } = string.Empty;
        
        public string CarYear { get; set; } = string.Empty;
        public double EnginePower { get; set; }
        public double EngineCoolantTemp { get; set; }
        public double EngineLoad { get; set; }
        public double EngineRPM { get; set; }
        public double AirIntakeTemp { get; set; }
        public double Speed { get; set; }
        public double ShortTermFuelBank1 { get; set; }
        public double throttlePosition { get; set; }
        public double TimingAdvance { get; set; }
        
        public string TroubleCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class DataFromCarDto
    {
        public string CarYear { get; set; } = string.Empty;
        public double EnginePower { get; set; }
        public double EngineCoolantTemp { get; set; }
        public double EngineLoad { get; set; }
        public double EngineRPM { get; set; }
        public double AirIntakeTemp { get; set; }
        public double Speed { get; set; }
        public double ShortTermFuelBank1 { get; set; }
        public double throttlePosition { get; set; }
        public double TimingAdvance { get; set; }
        
        public string TroubleCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}
