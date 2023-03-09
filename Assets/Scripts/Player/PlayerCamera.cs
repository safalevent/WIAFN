using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public LayerMask gunLookLayerMask;
    public Character Player;
    private bool _isShooting = false;

    public static event InteractHandler OnInteract;


    // Update is called once per frame
    public Camera fpsCam;
    void Update()
    {
        //Physics.Raycast(fpsCam.transform.position, fpsCam.transform.forward, out RaycastHit hit, 100.0f, gunLookLayerMask);
        Ray ray = new Ray(fpsCam.transform.position, fpsCam.transform.forward);
        //RotateGun(hit);
        // && hit.transform != null
        if (_isShooting)
        {
            Player.Weapon.TryShoot(ray.GetPoint(1000f));
        }

        if (Input.GetMouseButtonDown(0))
        {
            _isShooting = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            _isShooting = false;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if(OnInteract != null)
            {
                OnInteract();
            }
        }

        //three ifs, why?!
        if (Input.GetKeyDown(KeyCode.R) && Player.HasEffect())
        {
            if(!Player.Effect.InAnimation)
            {
                if (!Player.Effect.Enabled)
                {
                    Player.Effect.OnEffectStart();
                }
                else Player.Effect.OnEffectEnd();
            }
        }
    }

    private void RotateGun(RaycastHit hit)
    {
        Vector3 gunLocalPos = Player.Weapon.transform.parent.InverseTransformPoint(hit.point);
        if (hit.transform == null || Player.Weapon.transform.rotation.y < -15f)
        {
            gunLocalPos = Quaternion.Euler(new Vector3(0f, -30f, -90f)) * Vector3.forward;
        }
        Player.Weapon.transform.localRotation = Quaternion.LookRotation(gunLocalPos - Player.Weapon.transform.localPosition);
    }

    public delegate void InteractHandler();
}
