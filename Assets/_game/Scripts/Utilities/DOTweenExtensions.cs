using DG.Tweening;
using UnityEngine;

namespace Scripts.Utility
{
	[System.Serializable]
	public class DOTweenTransition
	{
		public float duration = 0.5f;
		public float delay;
		public Ease ease = Ease.Linear;
		public AnimationCurve easeCurve = AnimationCurve.Linear(0, 0, 1, 1);
		public float length => duration + delay;

		public Tweener Setup<T>(T value, System.Func<T, float, bool, Tweener> method, bool snapping = false)
		{
			var tweener = method?.Invoke(value, duration, snapping).SetDelay(delay);
			if (ease == Ease.Unset)
			{
				tweener.SetEase(easeCurve);
			}
			else
			{
				tweener.SetEase(ease);
			}
			return tweener;
		}

		public Tweener Setup<T>(T value, System.Func<T, float, Tweener> method)
		{
			var tweener = method?.Invoke(value, duration).SetDelay(delay);
			if (ease == Ease.Unset)
			{
				tweener.SetEase(easeCurve);
			}
			else
			{
				tweener.SetEase(ease);
			}
			return tweener;
		}

		public delegate Tweener V<T>(in T value, in float duration);
	}
}
