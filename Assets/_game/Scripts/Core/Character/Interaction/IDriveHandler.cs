namespace Core.Character.Interaction
{
    public interface IDriveHandler : ICharacterHandler
    {
        public float PitchAxis { get; set; }
        public float RollAxis { get; set; }
        public float YawAxis { get; set; }
        public float ThrustAxis { get; set; }
        public float SupportsPowerAxis { get; set; }
        public void ResetControls();
    }
}