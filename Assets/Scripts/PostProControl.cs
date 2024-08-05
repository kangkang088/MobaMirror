using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace MirrorExample
{
    public class PostProControl : MonoBehaviour
    {
        private static PostProControl instance;
        public static PostProControl Instance => instance;

        private void Awake()
        {
            instance = this;
        }

        private Vignette _vignette;
        private ColorGrading _colorGrading;

        private void Start()
        {
            GetComponent<PostProcessVolume>().profile.TryGetSettings(out _vignette);
            GetComponent<PostProcessVolume>().profile.TryGetSettings(out _colorGrading);
        }

        public void Hurted(float time)
        {
            StartCoroutine(ReallyHurted(time));
        }

        private IEnumerator ReallyHurted(float time)
        {
            while(time > 0)
            {
                _vignette.intensity.value = Mathf.Lerp(0,0.8f,time);
                yield return null;
                time -= Time.deltaTime;
            }
        }

        public void Dead()
        {
            _colorGrading.saturation.value = -100f;
        }

        public void Rebirth()
        {
            _colorGrading.saturation.value = 0f;
        }
    }
}
