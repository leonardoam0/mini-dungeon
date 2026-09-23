namespace Ruinas
{
    /// <summary>
    /// Acesso aos poucos serviços persistentes entre cenas. Sistemas de cena ficam em <see cref="LevelContext"/>.
    /// </summary>
    public static class Services
    {
        public static GameDatabase Database { get; internal set; }
        public static SettingsService Settings { get; internal set; }
        public static SaveService Save { get; internal set; }
        public static AudioService Audio { get; internal set; }
        public static InputRouter Input { get; internal set; }
        public static GameStateMachine State { get; internal set; }
        public static SceneFlow Flow { get; internal set; }
        public static LaunchArgs Args { get; internal set; }

        public static bool Ready => Database != null && Input != null && State != null;
    }
}
