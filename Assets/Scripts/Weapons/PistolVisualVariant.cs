using LastStep.Core;
using UnityEngine;

namespace LastStep.Weapons
{
    [DisallowMultipleComponent]
    public sealed class PistolVisualVariant : MonoBehaviour
    {
        private void Start()
        {
            var placeholderRenderer = GetComponent<Renderer>();
            if (placeholderRenderer != null) placeholderRenderer.enabled = false;
            if (GameManager.Instance == null) return;
            int index = GetComponentInParent<LastStep.Player.PlayerWeapon>() != null ? GameManager.Instance.PistolIndex : 0;
            Equip(index);
        }
        public void Equip(int index)
        {
            var previous = transform.Find("SelectedPistolMesh");
            if (previous != null) Destroy(previous.gameObject);
            var model = transform.Find("CinematicPistol");
            var imported = Resources.Load<GameObject>("Pistols/Pistol" + index);
            if (imported != null)
            {
                if (model != null) foreach (var oldRenderer in model.GetComponentsInChildren<Renderer>()) oldRenderer.enabled = false;
                var instance = Instantiate(imported, transform);
                instance.name = "SelectedPistolMesh";
                instance.transform.localPosition = new Vector3(0f, -.018f, .025f);
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one * .76f;
                return;
            }
            if (model == null) return;
            model.localScale = index == 1 ? new Vector3(.90f,.92f,.86f) :
                index == 2 ? new Vector3(1.12f,1.08f,1.17f) : Vector3.one;
            Color tint = index == 1 ? new Color(.19f,.22f,.25f) :
                index == 2 ? new Color(.075f,.085f,.105f) : new Color(.12f,.14f,.16f);
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                if (renderer.name.Contains("Grip") || renderer.name.Contains("Bore")) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
