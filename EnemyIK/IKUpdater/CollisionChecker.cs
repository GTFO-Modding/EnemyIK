using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EnemyIK.IKUpdater
{
    internal sealed class CollisionChecker : MonoBehaviour
    {
        public bool IsColliding = false;

        void OnCollisionEnter(Collision collision)
        {
            foreach (var col in collision.contacts)
            {
                if (col.otherCollider != null)
                {
                    Logger.Info(col.otherCollider.name);
                    Logger.Info(col.otherCollider.gameObject.layer);
                }
                    
            }
            IsColliding = true;
        }

        void OnCollisionExit(Collision collision)
        {
            IsColliding = false;
        }
    }
}
