using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    public Animator capAnimator;

    private void Start()
    {
        // Get existing Animator from the same GameObject
        capAnimator = GetComponent<Animator>();
    }

    public void PlayClapAnimation()
    {
        capAnimator.SetTrigger("Clap");
    }
}
