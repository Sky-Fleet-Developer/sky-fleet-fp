using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Runtime.UI
{
    public class SubMenuButton : Selectable, IPointerClickHandler, IDeselectHandler
    {
        public RectTransform targetMenu;

        protected override void Awake()
        {
            base.Awake();
            
        }

        private bool CurrentActivity => targetMenu.gameObject.activeInHierarchy;
    
        public void OnPointerClick(PointerEventData eventData)
        {
            SetState(!CurrentActivity);
        }
        
        /*public new void OnSelect(BaseEventData eventData)
        {
            Debug.Log("OnSelect");
            SetState(true);
        }*/
        
        public new async void OnDeselect(BaseEventData eventData)
        {
            while (Input.GetMouseButton(0))
            {
                await Task.Yield();
            }
            Debug.Log("OnDeselect");
            SetState(false);
        }

        private void SetState(bool activity)
        {
            targetMenu.gameObject.SetActive(activity);
        }
        
        
    }
}
