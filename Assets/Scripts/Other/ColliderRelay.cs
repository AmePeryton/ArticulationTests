using UnityEngine;
using UnityEngine.Events;

// Relay collider events on this GameObject to any other public method
[RequireComponent(typeof(Collider))]
public class ColliderRelay : MonoBehaviour
{
	public UnityEvent onCollisionEnter;
	public UnityEvent onCollisionExit;
	public UnityEvent onCollisionStay;
	public UnityEvent onMouseDown;
	public UnityEvent onMouseDrag;
	public UnityEvent onMouseEnter;
	public UnityEvent onMouseExit;
	public UnityEvent onMouseOver;
	public UnityEvent onMouseUp;
	public UnityEvent onParticleCollision;
	public UnityEvent onTriggerEnter;
	public UnityEvent onTriggerExit;
	public UnityEvent onTriggerStay;

	private void OnCollisionEnter(Collision collision)
	{
		onCollisionEnter.Invoke();
	}

	private void OnCollisionExit(Collision collision)
	{
		onCollisionExit.Invoke();
	}

	private void OnCollisionStay(Collision collision)
	{
		onCollisionStay.Invoke();
	}

	private void OnMouseDown()
	{
		onMouseDown.Invoke();
	}

	private void OnMouseDrag()
	{
		onMouseDrag.Invoke();
	}

	private void OnMouseEnter()
	{
		onMouseEnter.Invoke();
	}

	private void OnMouseExit()
	{
		onMouseExit.Invoke();
	}

	private void OnMouseOver()
	{
		onMouseOver.Invoke();
	}

	private void OnMouseUp()
	{
		onMouseUp.Invoke();
	}

	private void OnParticleCollision(GameObject other)
	{
		onParticleCollision.Invoke();
	}

	private void OnTriggerEnter(Collider other)
	{
		onTriggerEnter.Invoke();
	}

	private void OnTriggerExit(Collider other)
	{
		onTriggerExit.Invoke();
	}

	private void OnTriggerStay(Collider other)
	{
		onTriggerStay.Invoke();
	}
}
