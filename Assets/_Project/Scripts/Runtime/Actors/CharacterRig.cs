using UnityEngine;

namespace Ruinas
{
    /// <summary>
    /// Rig de peças rígidas: cada articulação é um pivô (ombro, quadril, pescoço) com a malha pendurada.
    /// Quadrúpedes usam armL/armR como patas dianteiras e legL/legR como traseiras.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        public Transform model;
        public Transform body;
        public Transform head;
        public Transform armL;
        public Transform armR;
        public Transform legL;
        public Transform legR;
        public Transform handR;
        public Transform handL;
        public Transform back;
        public Renderer[] renderers;

        [HideInInspector] public Quaternion bodyRest = Quaternion.identity, headRest = Quaternion.identity,
            armLRest = Quaternion.identity, armRRest = Quaternion.identity, legLRest = Quaternion.identity, legRRest = Quaternion.identity,
            modelRestRot = Quaternion.identity;
        [HideInInspector] public Vector3 modelRestPos;
        bool captured;

        void Awake() => CaptureRest();

        public void CaptureRest()
        {
            if (captured) return;
            captured = true;
            if (body) bodyRest = body.localRotation;
            if (head) headRest = head.localRotation;
            if (armL) armLRest = armL.localRotation;
            if (armR) armRRest = armR.localRotation;
            if (legL) legLRest = legL.localRotation;
            if (legR) legRRest = legR.localRotation;
            if (model) { modelRestPos = model.localPosition; modelRestRot = model.localRotation; }
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
        }

        public void SetVisible(bool visible)
        {
            if (renderers == null) return;
            foreach (var r in renderers) if (r != null) r.enabled = visible;
        }
    }
}
