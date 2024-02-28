using UnityEngine;

namespace Simulation {
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class Representation<T> : MonoBehaviour {
        public T RepresentedObject { get; private set; }
        protected SpriteRenderer SpriteRenderer { get; private set; }
        
        public void Initialize(T representedObject) {
            RepresentedObject = representedObject;
            SpriteRenderer = GetComponent<SpriteRenderer>();
            OnInitialize(representedObject);
        }

        protected abstract void OnInitialize(T representedObject);
    }
}