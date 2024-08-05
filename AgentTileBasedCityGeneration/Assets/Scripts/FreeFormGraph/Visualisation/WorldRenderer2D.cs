using UnityEngine;
using FreeFormGraph.World;

namespace FreeFormGraph.Visualisation {
    public class WorldRenderer2D : MonoBehaviour {
        public IWorld World;
        [SerializeField] public Material material;

        [SerializeField] private Color[] colors = {
            Color.blue,
            Color.green,
            Color.red,
            Color.white
        };

        private Gradient colorGradient;
        private Texture2D texture;

        private void Awake() {
            colorGradient = new Gradient();
            if (colors == null || colors.Length < 2) {
                colors = new Color[2];
                colors[0] = Color.white;
                colors[1] = Color.red;
            }

            var colorGradients = new GradientColorKey[colors.Length];
            for (var i = 0; i < colors.Length; i++)
                colorGradients[i] = new GradientColorKey(colors[i], (float)i / (colors.Length - 1));
            var alphas = new GradientAlphaKey[2];
            alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphas[1] = new GradientAlphaKey(1.0f, 1.0f);
            colorGradient.SetKeys(colorGradients, alphas);
        }

        private void Start() {
            World = FindObjectOfType<WorldGameObject>();
            RenderTexture();
        }

        private void RenderTexture() {
            var width = World.Width;
            var height = World.Height;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            var minHeight = World.MinHeight;
            var maxHeight = World.MaxHeight;

            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    var heightValue = Mathf.InverseLerp(minHeight, maxHeight, World.GetHeightAt(x, y));
                    var color = colorGradient.Evaluate(heightValue);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform);
            quad.transform.localPosition = new Vector3(width / 2.0f, height / 2.0f, 0);
            quad.transform.localScale = new Vector3(width, height, 1);

            var renderer = quad.GetComponent<Renderer>();
            renderer.material = material;
            renderer.material.mainTexture = texture;
        }
    }
}