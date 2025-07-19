using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Physic
{
    [DrawWithUnity]
    public class Rope : MonoBehaviour
    {
        [SerializeField, HideInInspector] private float length;
        [SerializeField] private int linksCount;
        [SerializeField] private Rigidbody connectedBody;
        [SerializeField] private float linkDrag;

        [ShowInInspector]
        public float Length
        {
            get => length;
            set
            {
                length = value;
                if (_links != null)
                {
                    float linkLength = length / (linksCount - 1);
                    for (var i = 1; i < _links.Length; i++)
                    {
                        _links[i].parentAnchorPosition = Vector3.down * linkLength;
                    }
                }
            }
        }
        private ArticulationBody[] _links;
        private HingeJoint _mainJoint;
        private HingeJoint _connectedJoint;
        public int LinksCount => linksCount;
        public LateEvent OnInitialize = new LateEvent();


        private void Awake()
        {
            Bootstrapper.OnLoadComplete.Subscribe(async () =>
            {
                await Task.Delay(10);
                bool isKinematic = connectedBody.isKinematic;
                connectedBody.isKinematic = true;
                _links = new ArticulationBody[linksCount];
                float linkLength = length / (linksCount - 1);
                for (var i = 0; i < _links.Length; i++)
                {
                    _links[i] = new GameObject($"Rope_Link_{i}").AddComponent<ArticulationBody>();
                    _links[i].jointType = ArticulationJointType.SphericalJoint;
                    _links[i].SnapAnchorToClosestContact();
                    _links[i].matchAnchors = false;
                    _links[i].transform.position = transform.position + Vector3.down * i * linkLength;
                    if (i > 0)
                    {
                        _links[i].transform.SetParent(_links[i-1].transform);
                        _links[i].parentAnchorPosition = Vector3.down * linkLength;
                    }
                    else
                    {
                        _links[i].transform.SetParent(transform);
                    }

                    _links[i].anchorPosition = Vector3.zero;
                    _links[i].linearDamping = linkDrag;
                    _links[i].angularDamping = linkDrag;
                    _links[i].WakeUp();
                }

                _mainJoint = connectedBody.gameObject.AddComponent<HingeJoint>();
                _mainJoint.connectedArticulationBody = _links[0];
                _mainJoint.autoConfigureConnectedAnchor = false;
                _mainJoint.anchor = connectedBody.transform.InverseTransformPoint(transform.position);
                _mainJoint.connectedAnchor = Vector3.zero;
                await Task.Delay(15);
                connectedBody.isKinematic = isKinematic;
                OnInitialize.Invoke();
            });
        }

        public IEnumerable<Vector3> GetJointsPoints()
        {
            Vector3 vector = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            for (var i = 0; i < _links.Length; i++)
            {
                rotation *= _links[i].transform.localRotation;
                vector += rotation * _links[i].transform.localPosition;
                yield return vector;
            }
        }

        public float GetLength()
        {
            float result = 0;// Vector3.Distance(_mainJoint.transform.position + _mainJoint.transform.TransformDirection(_mainJoint.anchor), _links[0].transform.position);
            for (var i = 1; i < _links.Length; i++)
            {
                result += Vector3.Distance(_links[i].transform.position, _links[i - 1].transform.position);
            }

            /*if (_connectedJoint)
            {
                result += Vector3.Distance(
                    _connectedJoint.transform.position + _connectedJoint.transform.TransformDirection(_connectedJoint.anchor),
                    _links[^1].transform.position);
            }*/

            return result;
        }

        private const float _massScaleFactor = 0.0015f;
        private const float _massScaleFactorInv = 1 - _massScaleFactor;
        public void Connect(Rigidbody target, Vector3 connectedAnchor)
        {
            _mainJoint.massScale = _massScaleFactorInv + connectedBody.mass * _massScaleFactor;
            _mainJoint.connectedMassScale = _massScaleFactorInv + target.mass * _massScaleFactor;
            target.transform.position += _links[^1].transform.position - target.transform.TransformPoint(connectedAnchor);
            _connectedJoint = target.gameObject.AddComponent<HingeJoint>();
            _connectedJoint.connectedArticulationBody = _links[^1];
            _connectedJoint.autoConfigureConnectedAnchor = false;
            _connectedJoint.anchor = connectedAnchor;
            _connectedJoint.connectedAnchor = Vector3.zero;
            _connectedJoint.massScale = _massScaleFactorInv + target.mass * _massScaleFactor;
            _connectedJoint.connectedMassScale = _massScaleFactorInv + connectedBody.mass * _massScaleFactor;
            TakeEasy(target);
        }

        private async void TakeEasy(Rigidbody target)
        {
            for (int i = 0; i < 100; i++)
            {
                var delta = _links[^1].transform.position -
                            target.transform.TransformPoint(_connectedJoint.anchor);
                target.transform.position += delta;
                target.velocity = _links[^1].velocity;
                await Task.Delay(1);
            }
        }
    }
}
