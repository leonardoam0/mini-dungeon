using UnityEngine;
using UnityEngine.InputSystem;

namespace Ruinas
{
    /// <summary>
    /// Traduz o InputRouter em comandos. O cursor é projetado na superfície real (raycast no cenário),
    /// não num plano y=0; cliques sobre a UI e o clique que fecha um menu não viram ações no mundo.
    /// </summary>
    public class LiveCommandSource : IPlayerCommandSource
    {
        static readonly RaycastHit[] hits = new RaycastHit[16];

        public bool Active => true;

        public void Fill(ref PlayerCommands c, PlayerController player)
        {
            var input = Services.Input;
            if (input == null || Services.State == null || !Services.State.AcceptsGameplayInput) return;

            c.gamepad = input.Device == ActiveDevice.Gamepad;
            c.move = input.MoveVector;
            c.aimStick = input.AimVector;

            bool pointerFree = !c.gamepad && !InputRouter.PointerOverUI() && !input.SuppressPointerUntilRelease;

            if (!c.gamepad)
            {
                var cam = player.ViewCamera;
                if (cam != null && Mouse.current != null)
                {
                    Ray ray = cam.ScreenPointToRay(input.PointerPosition);
                    c.hoverTarget = PickActor(ray, player.Actor);
                    if (RaycastGround(ray, out var ground))
                    {
                        c.hasAimPoint = true;
                        c.aimPoint = ground;
                    }
                    else
                    {
                        // Sem superfície sob o cursor: plano na altura do jogador.
                        var plane = new Plane(Vector3.up, player.transform.position);
                        if (plane.Raycast(ray, out float d)) { c.hasAimPoint = true; c.aimPoint = ray.GetPoint(d); }
                    }
                    if (c.hoverTarget != null) { c.hasAimPoint = true; c.aimPoint = c.hoverTarget.transform.position; }
                }
            }

            bool meleeAllowed = c.gamepad || pointerFree;
            c.meleePressed = meleeAllowed && input.Melee.WasPressedThisFrame();
            c.meleeHeld = meleeAllowed && input.Melee.IsPressed();
            bool rangedAllowed = c.gamepad || !InputRouter.PointerOverUI();
            c.rangedPressed = rangedAllowed && input.Ranged.WasPressedThisFrame();
            c.rangedHeld = rangedAllowed && input.Ranged.IsPressed();
            c.dodgePressed = input.Dodge.WasPressedThisFrame();
            c.potionPressed = input.Potion.WasPressedThisFrame();
            c.interactPressed = input.Interact.WasPressedThisFrame();
            c.artifact1 = input.Artifact1.WasPressedThisFrame();
            c.artifact2 = input.Artifact2.WasPressedThisFrame();
            c.artifact3 = input.Artifact3.WasPressedThisFrame();
            c.attackInPlace = input.AttackInPlace.IsPressed();

            if (!c.gamepad)
            {
                c.clickPressed = c.meleePressed;
                c.clickHeld = c.meleeHeld;
                c.clickHasPoint = c.hasAimPoint;
                c.clickPoint = c.aimPoint;
                c.clickTarget = c.hoverTarget;
                c.clickInteractable = c.hasAimPoint ? Interactable.FindNear(c.aimPoint, 1.4f) : null;
            }
        }

        static Actor PickActor(Ray ray, Actor self)
        {
            int n = Physics.SphereCastNonAlloc(ray, 0.35f, hits, 200f, Layers.ActorsMask, QueryTriggerInteraction.Collide);
            Actor best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var a = hits[i].collider.GetComponentInParent<Actor>();
                if (a == null || a == self || a.IsDead || !self.IsHostileTo(a)) continue;
                if (hits[i].distance < bestD) { bestD = hits[i].distance; best = a; }
            }
            return best;
        }

        public static bool RaycastGround(Ray ray, out Vector3 point)
        {
            if (Physics.Raycast(ray, out var hit, 250f, Layers.EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                return true;
            }
            point = default;
            return false;
        }
    }
}
