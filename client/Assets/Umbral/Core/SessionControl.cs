using System;

namespace Umbral.Core
{
    /// <summary>Orden de la tablet del asesor al visor. Sin descuentos: fuera del slice (CLAUDE.md).</summary>
    [Serializable]
    public class SessionCommand
    {
        public const string PauseKind = "pause";
        public const string ResumeKind = "resume";
        public const string JumpKind = "jump";

        public string kind;
        public int stage; // solo para jump

        public static SessionCommand Pause() => new SessionCommand { kind = PauseKind };
        public static SessionCommand Resume() => new SessionCommand { kind = ResumeKind };
        public static SessionCommand Jump(SessionStage stage) => new SessionCommand { kind = JumpKind, stage = (int)stage };
    }

    /// <summary>Estado de pausa y resolución de comandos del asesor. C# puro: el visor aplica los efectos (negro, interactores).</summary>
    public class SessionControl
    {
        public bool Paused { get; private set; }

        public event Action Changed;

        /// <summary>Aplica el comando. Devuelve false si se ignora (desconocido, sin cambio, salto inválido o en pausa).</summary>
        public bool Apply(SessionCommand command, Action<SessionStage> jump)
        {
            if (command == null) return false;
            switch (command.kind)
            {
                case SessionCommand.PauseKind: return SetPaused(true);
                case SessionCommand.ResumeKind: return SetPaused(false);
                case SessionCommand.JumpKind:
                    if (Paused) return false; // primero se reanuda
                    if (command.stage < 0 || command.stage >= SessionFlow.StageCount) return false;
                    jump?.Invoke((SessionStage)command.stage);
                    return true;
                default: return false;
            }
        }

        public bool SetPaused(bool value)
        {
            if (Paused == value) return false;
            Paused = value;
            Changed?.Invoke();
            return true;
        }
    }
}
