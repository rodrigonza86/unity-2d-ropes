using System.Collections.Generic;
using UnityEngine;

namespace Soulbound
{
    [System.Serializable]
    public class RopeBreakablePart
    {
        public float timeToBreak;
        public int index;
    }

    public enum FaceDirections { Left, Right, PlayerDirection }

    public class Rope : MonoBehaviour
    {
        [Header("References:")]
        public Rigidbody2D hook;
        public List<GameObject> linkPrefabs;
        public GameObject endingPrefab;        
        public GameObject breakPrefab;

        [Space]
        [Header("Configuration:")]        
        public int links = 7;
        public float linkDistance = .5f;        

        [Space]
        public List<RopeBreakablePart> breakableParts;

        [HideInInspector]
        public bool isCharacterInRope;
        [HideInInspector]
        public int currentSegment;        
        [HideInInspector]
        public bool isGenerated;
        [HideInInspector]
        public Dictionary<int, GameObject> partReferences;

        protected Transform _transform;        
        private List<int> toRemove;
        private float currentTimeInRope;       

        private void Start()
        {
            _transform = transform;
            partReferences = new Dictionary<int, GameObject>();
            toRemove = new List<int>();

            breakableParts.Sort((p1, p2) => -p1.index.CompareTo(p2.index));

            GenerateRope();
        }

        private void Update()
        {
            if (isCharacterInRope)
            {
                currentTimeInRope += Time.deltaTime;
                for (int i = 0; i < breakableParts.Count; i++)
                {
                    var part = breakableParts[i];

                    // Only break if is in top of user
                    if (part.index > currentSegment - 1)
                        continue;
                    
                    if (part.timeToBreak <= currentTimeInRope)
                    {
                        var nextPart = partReferences[part.index + 1];
                        var distanceJoint = nextPart.GetComponent<DistanceJoint2D>();                        
                        distanceJoint.enabled = false;
                        distanceJoint.connectedBody = null;                        

                        var link = partReferences[part.index];
                        Destroy(link);
                        partReferences.Remove(part.index);
                        toRemove.Add(i);

                        // Release player from rope                        
                        currentTimeInRope = 0;
                        break;
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    breakableParts.RemoveAt(toRemove[i]);
                }

                toRemove.Clear();
            }
            else
            {
                currentTimeInRope = 0;
            }
        }        

        private void GenerateRope()
        {
            var previousRigidBody = hook;            
            for (int i = 0; i < links; i++)
            {               
                GameObject link;
                var linkPrefab = linkPrefabs[Random.Range(0, linkPrefabs.Count)];

                if (i == links - 1)
                    link = Instantiate(endingPrefab, _transform);
                else if (breakableParts.FindIndex(p => p.index == i) != -1)
                    link = Instantiate(breakPrefab, _transform);
                else
                    link = Instantiate(linkPrefab, _transform);

                link.transform.position = previousRigidBody.transform.position + (-Vector3.up * linkDistance);

                var joint = link.GetComponent<HingeJoint2D>();
                joint.connectedBody = previousRigidBody;
                joint.connectedAnchor = new Vector2(0, -linkDistance / 2);
                joint.anchor = new Vector2(0, linkDistance / 2);
                joint.enabled = true;

                var distanceJoint = link.AddComponent<DistanceJoint2D>();
                distanceJoint.enabled = true;
                distanceJoint.connectedBody = previousRigidBody;
                distanceJoint.autoConfigureDistance = true;

                previousRigidBody = link.GetComponent<Rigidbody2D>();

                if (i == links - 1)
                {
                    previousRigidBody.mass = 30;
                    previousRigidBody.gravityScale = 3;                   
                }

                var segment = link.GetComponent<RopeSegment>();
                if (segment != null)
                {
                    segment.Rope = this;
                    segment.SegmentIndex = i;
                }

                partReferences.Add(i, link);
            }

            isGenerated = true;
        }

        private void Rebuild()
        {
            isGenerated = false;
            foreach (var part in partReferences.Values)
            {
                Destroy(part);
            }
            partReferences.Clear();
            toRemove.Clear();
            currentTimeInRope = 0;
            isCharacterInRope = false;
            currentSegment = 0;                    

            GenerateRope();
        }

        private void OnDrawGizmos()
        {            
            var target = new Vector3(hook.transform.position.x, hook.transform.position.y - (linkDistance * links), 0);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(hook.transform.position, target);

            Gizmos.color = Color.red;
            foreach (var part in breakableParts)
            {
                var breakPosition = new Vector3(hook.transform.position.x, hook.transform.position.y - (linkDistance * (part.index + 1)), 0);
                Gizmos.DrawSphere(breakPosition, .1f);
            }
        }
    }
}