//-----------------------------------------------------------------------
// <copyright file="PawnManipulator.cs" company="Google LLC">
//
// Copyright 2019 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace GoogleARCore.Examples.ObjectManipulation
{
    using GoogleARCore;
    using UnityEngine;

    /// <summary>
    /// Controls the placement of objects via a tap gesture.
    /// </summary>
    public class PawnManipulator : Manipulator
    {
        /// <summary>
        /// The first-person camera being used to render the passthrough camera image (i.e. AR
        /// background).
        /// </summary>
        public Camera FirstPersonCamera;

        /// <summary>
        /// A prefab to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject PawnPrefab;

        /// <summary>
        /// A prefab to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject ObjectForVerticalWall;

        /// <summary>
        /// A prefab to place when a raycast from a user touch hits a plane.
        /// </summary>
        public GameObject ObjectForCeiling;

        public GameObject BubbleSpeech;

        /// <summary>
        /// Manipulator prefab to attach placed objects to.
        /// </summary>
        public GameObject ManipulatorPrefab;

        public GameObject ObjectMenu;

        private void Start()
        {
            
        }

        /// <summary>
        /// Returns true if the manipulation can be started for the given gesture.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        /// <returns>True if the manipulation can be started.</returns>
        protected override bool CanStartManipulationForGesture(TapGesture gesture)
        {
            if (SlideUpPanel.wasClickedOnUI == true)
            {
                return false;
            }
            if (gesture.TargetObject == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Function called when the manipulation is ended.
        /// </summary>
        /// <param name="gesture">The current gesture.</param>
        protected override void OnEndManipulation(TapGesture gesture)
        {
            if (gesture.WasCancelled)
            {
                return;
            }

            // If gesture is targeting an existing object we are done.
            if (gesture.TargetObject != null)
            {
                return;
            }

            // Raycast against the location the player touched to search for planes.
            TrackableHit hit;
            TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon;

            if (Frame.Raycast(
                gesture.StartPosition.x, gesture.StartPosition.y, raycastFilter, out hit))
            {
                // Use hit pose and camera pose to check if hittest is from the
                // back of the plane, if it is, no need to create the anchor.
                Debug.Log("gesture position " + gesture.StartPosition.ToString()); 
                if ((hit.Trackable is DetectedPlane) &&
                    Vector3.Dot(FirstPersonCamera.transform.position - hit.Pose.position,
                        hit.Pose.rotation * Vector3.up) < 0)
                {
                    Debug.Log("Hit at back of the current DetectedPlane");
                }
                else
                {
                    GameObject objectPrefab = null;
                    DetectedPlane plane = hit.Trackable as DetectedPlane;
                    Quaternion rotation = hit.Pose.rotation;
                    switch (plane.PlaneType)
                    {
                        case DetectedPlaneType.Vertical:
                            objectPrefab = ObjectForVerticalWall;
                            rotation = Quaternion.LookRotation(Vector3.down, rotation * Vector3.up); 
                            break;
                        case DetectedPlaneType.HorizontalUpwardFacing:
                            objectPrefab = PawnPrefab;
                            
                            break;
                        case DetectedPlaneType.HorizontalDownwardFacing:
                            objectPrefab = PawnPrefab;
                            break;
                    }

                    var planeWithTypeDict = UpdateFloorOfTheHouse.planeWithTypeDict;
                    if (planeWithTypeDict.ContainsKey(hit.Trackable as DetectedPlane)) {
                        Debug.Log("Plane Type: " + planeWithTypeDict[hit.Trackable as DetectedPlane]);
                    }
                    // Instantiate game object at the hit pose.
                    var gameObject = Instantiate(objectPrefab, hit.Pose.position, rotation);
                    // var newPosition = new Vector3(hit.Pose.position.x, hit.Pose.position.y + 0.2f, hit.Pose.position.z);
                    // var gameObject = Instantiate(BubbleSpeech, newPosition, rotation);
                    // Debug.Log("hit pose postion" + hit.Pose.position);
                    // var gameObject = Instantiate(objectPrefab, hit.Pose.position, Quaternion.Euler(90, 0, 0));

                    // Instantiate manipulator.
                    //var manipulator =
                    //    Instantiate(ManipulatorPrefab, hit.Pose.position, hit.Pose.rotation);
                    var manipulator =
                        Instantiate(ManipulatorPrefab, hit.Pose.position, rotation);

                    // var menuPostion = new Vector3(hit.Pose.position.x, hit.Pose.position.y + 0.5f, hit.Pose.position.z);
                    // var menu = Instantiate(ObjectMenu, menuPostion, Quaternion.identity);

                    // Make game object a child of the manipulator.
                    gameObject.transform.parent = manipulator.transform;

                    // Create an anchor to allow ARCore to track the hitpoint as understanding of
                    // the physical world evolves.
                    var anchor = hit.Trackable.CreateAnchor(hit.Pose);

                    // Make manipulator a child of the anchor.
                    manipulator.transform.parent = anchor.transform;
                    // menu.transform.parent = anchor.transform;

                    // Select the placed object.
                    manipulator.GetComponent<Manipulator>().Select();
                }
            }
        }
    }
}
