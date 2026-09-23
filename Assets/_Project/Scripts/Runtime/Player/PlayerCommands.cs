using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Comandos do jogador em um quadro. Preenchidos pela entrada ao vivo, pelo roteiro de referência
    /// ou pelo piloto automático — sempre consumidos pelos mesmos sistemas reais.
    /// </summary>
    public struct PlayerCommands
    {
        public Vector2 move;
        public Vector2 aimStick;
        public bool hasAimPoint;
        public Vector3 aimPoint;
        public Actor hoverTarget;

        public bool meleePressed, meleeHeld;
        public bool rangedPressed, rangedHeld;
        public bool dodgePressed;
        public bool potionPressed;
        public bool interactPressed;
        public bool artifact1, artifact2, artifact3;
        public bool attackInPlace;

        public bool clickPressed, clickHeld;
        public bool clickHasPoint;
        public Vector3 clickPoint;
        public Actor clickTarget;
        public Interactable clickInteractable;

        public bool gamepad;
        /// <summary>Movimento já em coordenadas do mundo (usado pelo piloto automático).</summary>
        public bool moveIsWorld;
        public Vector3 moveWorld;
    }

    public interface IPlayerCommandSource
    {
        bool Active { get; }
        void Fill(ref PlayerCommands commands, PlayerController player);
    }
}
