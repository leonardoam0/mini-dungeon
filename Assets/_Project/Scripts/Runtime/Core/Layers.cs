using UnityEngine;

namespace Ruinas
{
    /// <summary>Camadas físicas do projeto (configuradas também pelo ProjectSetup no editor).</summary>
    public static class Layers
    {
        public const int Default = 0;
        public const int Environment = 6;
        public const int Actors = 7;
        public const int InvisibleWall = 8;
        public const int Interactable = 9;
        public const int Pickup = 10;
        public const int Projectile = 11;
        public const int Vfx = 12;

        public static readonly string[] Names =
        {
            null, null, null, null, null, null,
            "Environment", "Actors", "InvisibleWall", "Interactable", "Pickup", "Projectile", "Vfx"
        };

        public const int EnvironmentMask = 1 << Environment;
        public const int ActorsMask = 1 << Actors;
        public const int GroundQueryMask = (1 << Environment) | (1 << Default);
        public const int MovementBlockMask = (1 << Environment) | (1 << InvisibleWall) | (1 << Default);

        /// <summary>Matriz de colisão: atores colidem com cenário, paredes invisíveis e outros atores.</summary>
        public static void ConfigureCollisionMatrix()
        {
            int[] custom = { Environment, Actors, InvisibleWall, Interactable, Pickup, Projectile, Vfx };
            foreach (int a in custom)
            {
                for (int b = 0; b < 32; b++)
                {
                    bool collide = Collides(a, b);
                    Physics.IgnoreLayerCollision(a, b, !collide);
                }
            }
        }

        static bool Collides(int a, int b)
        {
            bool Pair(int x, int y) => (a == x && b == y) || (a == y && b == x);
            if (Pair(Actors, Environment) || Pair(Actors, InvisibleWall) || Pair(Actors, Actors) || Pair(Actors, Default)) return true;
            if (Pair(Environment, Environment) || Pair(Environment, Default)) return true;
            return false;
        }
    }
}
