/*
 * Copyright (c) 2021 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * Notwithstanding the foregoing, you may not use, copy, modify, merge, publish, 
 * distribute, sublicense, create a derivative work, and/or sell copies of the 
 * Software in any work that is designed, intended, or marketed for pedagogical or 
 * instructional purposes related to programming, coding, application development, 
 * or information technology.  Permission for such use, copying, modification,
 * merger, publication, distribution, sublicensing, creation of derivative works, 
 * or sale is expressly withheld.
 *    
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
*/
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lander : MonoBehaviour
{
    // Public members

    public bool allowThrust;

    public Transform bottomThruster;

    public GameObject explosionPrefab;

    public float fuel;

    public Text fuelText;

    public Slider fuelSlider;

    public Transform leftThruster;

    public Animator leftThrusterAnim;

    public Animator mainThrusterAnim;

    public float mainThrustPower;

    public Transform rightThruster;

    public Animator rightThrusterAnim;

    public float sideThrustPower;

    public AudioSource thrusterAudio;

    public UnityEvent OnLanderDestroyed;
    public UnityEvent OnLanded;

    // Private members

    private bool canDeployFeet;

    private GameObject landerObjective;

    private Rigidbody2D landerRigidBody2D;

    private HingeJoint2D feetJoint;

    private bool shouldPlayThrustSfx;

    // Use this for initialization
    void Start()
    {
        landerRigidBody2D = GetComponent<Rigidbody2D>();
        landerObjective = GameObject.Find("LanderObjective");
        feetJoint = transform.Find("LanderFeet").GetComponent<HingeJoint2D>();
    }


    private void Restart()
    {
        SceneManager.LoadScene(0);
    }

    // Update is called once per frame
    void Update()
    {
        var objectiveDistance = Vector2.Distance(transform.position, landerObjective.transform.position);

        if (objectiveDistance <= 1f && canDeployFeet == false)
        {
            canDeployFeet = true;
        }

        if (canDeployFeet)
        {
            var landerFeetYPos = feetJoint.anchor.y + Time.deltaTime / 3;

            if (landerFeetYPos < 0.38f)
            {
                feetJoint.anchor = new Vector2(0f, landerFeetYPos);
            }
            else
            {
                canDeployFeet = false;
            }
        }

        if (shouldPlayThrustSfx)
        {
            PlayThrusterSfx();
        }
        else
        {
            thrusterAudio.Pause();
        }
    }

    void FixedUpdate()
    {
        shouldPlayThrustSfx = false;
        
        if (!allowThrust || fuel <= 0)
        {
            mainThrusterAnim.SetBool("ApplyingThrust", false);
            leftThrusterAnim.SetBool("ApplyingThrust", false);
            rightThrusterAnim.SetBool("ApplyingThrust", false);
            return;
        }

        if (Input.GetAxis("Vertical") > 0)
        {
            shouldPlayThrustSfx = true;
            ApplyForce(bottomThruster, mainThrustPower);
            if (mainThrusterAnim != null && mainThrusterAnim.runtimeAnimatorController != null)
            {
                mainThrusterAnim.SetBool("ApplyingThrust", true);
            }
        }
        else
        {
            if (mainThrusterAnim != null && mainThrusterAnim.runtimeAnimatorController != null)
            {
                mainThrusterAnim.SetBool("ApplyingThrust", false);
            }
        }

        if (Input.GetAxis("Horizontal") > 0)
        {
            shouldPlayThrustSfx = true;
            ApplyForce(leftThruster, sideThrustPower);
            if (leftThrusterAnim != null && leftThrusterAnim.runtimeAnimatorController != null)
            {
                leftThrusterAnim.SetBool("ApplyingThrust", true);
            }
        }
        else
        {
            if (leftThrusterAnim != null && leftThrusterAnim.runtimeAnimatorController != null)
            {
                leftThrusterAnim.SetBool("ApplyingThrust", false);
            }
        }

        if (Input.GetAxis("Horizontal") < 0)
        {
            shouldPlayThrustSfx = true;
            ApplyForce(rightThruster, sideThrustPower);
            if (rightThrusterAnim != null && rightThrusterAnim.runtimeAnimatorController != null)
            {
                rightThrusterAnim.SetBool("ApplyingThrust", true);
            }
        }
        else
        {
            if (rightThrusterAnim != null && rightThrusterAnim.runtimeAnimatorController != null)
            {
                rightThrusterAnim.SetBool("ApplyingThrust", false);
            }
        }
    }

    private void PlayThrusterSfx()
    {
        if (thrusterAudio.isPlaying)
        {
            return;
        }

        thrusterAudio.Play();
    }

    private void ApplyForce(Transform thrusterTransform, float thrustPower)
    {
        if (allowThrust && fuel > 0f)
        {
            var direction = transform.position - thrusterTransform.position;
            landerRigidBody2D.AddForceAtPosition(direction.normalized * thrustPower, thrusterTransform.position);
            fuel -= 0.01f;
            fuelSlider.value = fuel;
            fuelText.text = "Fuel " + Mathf.Round(fuel);
        }
    }

    void OnCollisionEnter2D(Collision2D hitInfo)
    {
        if (hitInfo.relativeVelocity.magnitude > 1)
        {
            HandleLanderDestroy();
        }
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag == "Fuel")
        {
            fuel += 10f;
            Destroy(collider.gameObject);
            if (fuel > fuelSlider.maxValue)
            {
                fuelSlider.maxValue = fuel;
            }
            fuelSlider.value = fuel;
        }
    }

    public void DetectLanding()
    {
        StartCoroutine(CheckLanded());
    }

    private IEnumerator CheckLanded()
    {
        while(landerRigidBody2D.velocity.magnitude > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        OnLanded.Invoke();
    }

    private void HandleLanderDestroy()
    {
        if (explosionPrefab != null)
        {
            var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 1f);
        }

        OnLanderDestroyed.Invoke();
        Destroy(gameObject);
    }
}