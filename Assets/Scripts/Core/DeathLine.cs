using UnityEngine;

namespace MergeGame.Core
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class DeathLine : MonoBehaviour
    {
        private void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var ball = other.GetComponent<BallController>();
            if (ball != null)
            {
                ball.SetAboveDeathLine(true);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var ball = other.GetComponent<BallController>();
            if (ball != null)
            {
                ball.SetAboveDeathLine(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var ball = other.GetComponent<BallController>();
            if (ball != null)
            {
                ball.SetAboveDeathLine(false);
            }
        }
    }
}
