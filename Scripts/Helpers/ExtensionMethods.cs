#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Hastable = ExitGames.Client.Photon.Hashtable;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading.Tasks;

namespace ChichoExtensions
{
    public static partial class ExtensionMethods
    {
        private static int HexToDec(string hex)
        {
            return Convert.ToInt32(hex, 16);
        }

        private static string DecToHex(int value)
        {
            return value.ToString("X2");
        }

        private static string NormailizedFloatToHex(float value)
        {
            return DecToHex(Mathf.RoundToInt(value * 255));
        }

        private static float HexToNormalizedFloat(string hex)
        {
            return HexToDec(hex) / 255f;
        }

        public static Color ColorFromHex(string hexString)
        {
            float red = HexToNormalizedFloat(hexString.Substring(0, 2));
            float green = HexToNormalizedFloat(hexString.Substring(2, 2));
            float blue = HexToNormalizedFloat(hexString.Substring(4, 2));

            return new Color(red, green, blue, hexString.Length >= 8 ? HexToNormalizedFloat(hexString.Substring(6, 2)) : 1);
        }

        public static string HexFromColor(Color color, bool useAlpha = false)
        {
            string red = NormailizedFloatToHex(color.r);
            string green = NormailizedFloatToHex(color.g);
            string blue = NormailizedFloatToHex(color.b);

            return red + green + blue + (useAlpha ? NormailizedFloatToHex(color.a) : string.Empty);
        }

        public static void SpeedMeasurer(Action action)
        {
            long before = GC.GetTotalMemory(false);
            var temp = Time.realtimeSinceStartup;

            action?.Invoke();

            long after = GC.GetTotalMemory(false);
            long diff = after - before;

            Debug.Log("Allocated bytes = " + diff.ToString() + 'B');
            Debug.Log(((Time.realtimeSinceStartup - temp) * 1000).ToString("f6") + "ms");
        }

        public static void SpeedMeasurer<T>(Action<T> action, T t)
        {
            long before = GC.GetTotalMemory(false);
            var temp = Time.realtimeSinceStartup;

            action?.Invoke(t);

            long after = GC.GetTotalMemory(false);
            long diff = after - before;

            Debug.Log("Allocated bytes = " + diff.ToString() + 'B');
            Debug.Log(((Time.realtimeSinceStartup - temp) * 1000).ToString("f6") + "ms");
        }

        public static void SpeedMeasurer<T, U>(Action<T, U> action, T t, U u)
        {
            long before = GC.GetTotalMemory(false);
            var temp = Time.realtimeSinceStartup;

            action?.Invoke(t, u);

            long after = GC.GetTotalMemory(false);
            long diff = after - before;

            Debug.Log("Allocated bytes = " + diff.ToString() + 'B');
            Debug.Log(((Time.realtimeSinceStartup - temp) * 1000).ToString("f6") + "ms");
        }

        public static void SpeedMeasurer<T, U, V>(Action<T, U, V> action, T t, U u, V v)
        {
            long before = GC.GetTotalMemory(false);
            var temp = Time.realtimeSinceStartup;

            action?.Invoke(t, u, v);

            long after = GC.GetTotalMemory(false);
            long diff = after - before;

            Debug.Log("Allocated bytes = " + diff.ToString() + 'B');
            Debug.Log(((Time.realtimeSinceStartup - temp) * 1000).ToString("f6") + "ms");
        }

        public static T SpeedMeasurer<T>(Func<T> func)
        {
            long before = GC.GetTotalMemory(false);
            var temp = Time.realtimeSinceStartup;

            var result = func.Invoke();

            long after = GC.GetTotalMemory(false);
            long diff = after - before;

            Debug.Log("Allocated bytes = " + diff.ToString() + 'B');
            Debug.Log(((Time.realtimeSinceStartup - temp) * 1000).ToString("f6") + "ms");

            return result;
        }

        public static U SpeedMeasurer<T, U>(Func<T, U> func, T t)
        {
            long before = GC.GetTotalMemory(false);
            var temp = Time.realtimeSinceStartup;

            var result = func.Invoke(t);

            long after = GC.GetTotalMemory(false);
            long diff = after - before;

            Debug.Log("Allocated bytes = " + diff.ToString() + 'B');
            Debug.Log(((Time.realtimeSinceStartup - temp) * 1000).ToString("f6") + "ms");

            return result;
        }

        public static V SpeedMeasurer<T, U, V>(Func<T, U, V> func, T t, U u)
        {
            long before = GC.GetTotalMemory(false);
            var temp = Time.realtimeSinceStartup;

            var result = func.Invoke(t, u);

            long after = GC.GetTotalMemory(false);
            long diff = after - before;

            Debug.Log("Allocated bytes = " + diff.ToString() + 'B');
            Debug.Log(((Time.realtimeSinceStartup - temp) * 1000).ToString("f6") + "ms");

            return result;
        }

        public static Vector2 GetClosestTo(this List<Vector2> vectors, Vector2 referencePoint)
        {
            float closetsDistance = Mathf.Infinity;
            Vector2 closestPlayer = Vector2.one * int.MaxValue;

            foreach (var vector in vectors)
            {
                if (vector == referencePoint) continue;

                float objectDistance = Vector2.Distance(referencePoint, vector);

                if (objectDistance < closetsDistance)
                {
                    closestPlayer = vector;
                    closetsDistance = objectDistance;
                }

            }

            return closestPlayer;
        }

        public static Vector2 GetFurthestTo(this List<Vector2> vectors, Vector2 referencePoint)
        {
            float furthestDistance = 0;
            Vector2 furthestPlayer = Vector2.zero;

            foreach (var vector in vectors)
            {
                if (vector == referencePoint) continue;

                float objectDistance = Vector2.Distance(referencePoint, vector);

                if (objectDistance > furthestDistance)
                {
                    furthestPlayer = vector;
                    furthestDistance = objectDistance;
                }

            }

            return furthestPlayer;
        }

        public static Vector2[] GetLinePoints(Vector2 startPoint, Vector2 endPoint, int numberOfPoints = 10)
        {
            Vector2 displacement = endPoint - startPoint;

            Vector2[] results = new Vector2[numberOfPoints];

            for (int i = 0; i < numberOfPoints; i++)
            {
                results[i] = startPoint + (displacement / numberOfPoints) * i;
            }

            return results;
        }

        public static List<T> GetOutRangeList<T>(List<T> objects, Vector2 referencePoint, float range) where T : MonoBehaviour
        {
            List<T> results = new List<T>();

            foreach (var @object in objects)
            {
                if ((Vector2)@object.transform.position == referencePoint) continue;

                float objectDistance = Vector2.Distance(referencePoint, @object.transform.position);

                if (objectDistance > range) results.Add(@object);
            }

            return results;
        }

        public static List<T> GetInRangeList<T>(List<T> objects, Vector2 referencePoint, float range) where T : MonoBehaviour
        {
            List<T> results = new List<T>();

            foreach (var @object in objects)
            {
                if ((Vector2)@object.transform.position == referencePoint) continue;

                float objectDistance = Vector2.Distance(referencePoint, @object.transform.position);

                if (objectDistance < range) results.Add(@object);
            }

            return results;
        }

        public static Vector2 GetCenterPoint(List<Vector2> points) => points.Count > 0 ? new Vector2(points.Average(t => t.x), points.Average(t => t.y)) : Vector2.zero;

        public static Vector2 GetCenterPoint(Vector2[] points) => points.Length > 0 ? new Vector2(points.Average(t => t.x), points.Average(t => t.y)) : Vector2.zero;

        public static Vector2 GetNormal(this Vector2 vector)
        {
            return new Vector2(-vector.y, vector.x);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="angle">Angle in radians of the desired vector</param>
        /// <param name="magnitude"></param>
        /// <returns></returns>
        public static Vector2 VectorFromPolar(float angle, float magnitude = 1)
        {
            return new Vector2(magnitude * Mathf.Cos(angle), magnitude * Mathf.Sin(angle));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <returns>Returns the Z angle in radians of the vector</returns>
        public static float Angle(this Vector2 vector)
        {
            return Mathf.Atan2(vector.y, vector.x);
        }

        /// <summary>
        /// Draws the vector and returns it
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="startPos"></param>
        /// <returns></returns>
        public static Vector3 DrawVector(this Vector3 vector, Vector3 startPos, float duration = 5f)
        {
            Debug.DrawLine(startPos, startPos + vector, Color.green, duration);
            return vector;
        }


        public static Keyframe GetHighestKey(this AnimationCurve curve, int index = 0)
        {
            List<Keyframe> selectedFrames = new List<Keyframe>();

            for (int i = 0; i < curve.keys.Length; i++)
            {
                Keyframe previousKey = i > 0 ? curve.keys[i - 1] : new Keyframe(0f, 0f);
                Keyframe currentKey = curve.keys[i];

                if (currentKey.value.AproximatelyEquals(previousKey.value))
                {
                    selectedFrames.Add(currentKey);
                    continue;
                }

                if (currentKey.value > previousKey.value)
                {
                    selectedFrames.Add(currentKey);
                    selectedFrames.Remove(previousKey);
                    continue;
                }

            }

            return selectedFrames[index];
        }

        public static Keyframe GetLowestKey(this AnimationCurve curve, int index = 0)
        {
            List<Keyframe> selectedFrames = new List<Keyframe>();

            for (int i = 0; i < curve.keys.Length; i++)
            {
                Keyframe previousKey = i > 0 ? curve.keys[i - 1] : new Keyframe(0f, 0f);
                Keyframe currentKey = curve.keys[i];

                if (currentKey.value.AproximatelyEquals(previousKey.value))
                {
                    selectedFrames.Add(currentKey);
                    continue;
                }

                if (currentKey.value < previousKey.value)
                {
                    selectedFrames.Add(currentKey);
                    selectedFrames.Remove(previousKey);
                    continue;
                }

            }

            return selectedFrames[index];
        }


        public static bool AproximatelyEquals(this float f1, float f2, float threshold = 0.08f)
        {
            float dif = Mathf.Abs(f1 - f2);

            return dif <= threshold;
        }

        /// <summary>
        /// Calculates the possible ways to sum up to <paramref name="sum"/> with the given range of numbers and the amount of factors
        /// </summary>
        /// <param name="sum">Number to sum up to with the numbers contained in the interval between <paramref name="minFactor"/> and <paramref name="maxFactor"/> using as many numbers as <paramref name="factorsAmount"/></param>
        /// <param name="minFactor">Lowest factor of the interval (Inclusive)</param>
        /// <param name="maxFactor">Highest factor of the interval (Exclusive)</param>
        /// <param name="factorsAmount">Amount of factors to sum up to <paramref name="sum"/></param>
        /// <returns></returns>
        public static int WaysToSumUpTo(int sum, int minFactor, int maxFactor, int factorsAmount)
        {
            int numberOfWays = 0;

            int[] possibleFactors = new int[maxFactor - minFactor];

            for (int i = 0; i < possibleFactors.Length; i++)
            {
                possibleFactors[i] = minFactor + i;
            }

            var factorsSelection = possibleFactors.Where(f => f < sum);
            if (factorsSelection.Count() < 1) return 0;

            int max = factorsSelection.Max();
            bool lessThanMax = sum < max;

            numberOfWays = max - (minFactor - 1) + (lessThanMax ? (factorsAmount - 2) : (-factorsAmount + 2));

            return numberOfWays;
        }

        public static int WaysToSumUpTo(int sum, int[] possibleFactors, int factorsAmount)
        {
            int numberOfWays = 0;

            var factorsSelection = possibleFactors.Where(f => f < sum);
            if (factorsSelection.Count() < 1) return 0;

            int max = factorsSelection.Max();
            int min = possibleFactors.Min();
            bool lessThanMax = sum < max;

            numberOfWays = max - (min - 1) + (lessThanMax ? (factorsAmount - 2) : (-factorsAmount + 2));

            Dictionary<int, bool> numChecked = new Dictionary<int, bool>();

            for (int i = min; i < max; i++)
            {
                int otherFactor = sum - i;
                numChecked.Add(otherFactor, false);

                if (!possibleFactors.Contains(otherFactor) && !numChecked[otherFactor])
                {
                    numberOfWays--;
                    numChecked[otherFactor] = true;
                }
            }

            return numberOfWays;
        }

        public static Vector2[] PointsInside(this Collider2D collider, float density = 1f)
        {
            Vector2 uppermostLeft = new Vector2(collider.bounds.min.x, collider.bounds.max.y);
            Vector2 uppermostRight = new Vector2(collider.bounds.max.x, collider.bounds.max.y);
            Vector2 downermostLeft = new Vector2(collider.bounds.min.x, collider.bounds.min.y);

            List<Vector2> innerPoints = new List<Vector2>();

            for (float i = uppermostLeft.x; i <= uppermostRight.x; i += 1 / density)
            {
                for (float j = uppermostLeft.y; j >= downermostLeft.y; j -= 1 / density)
                {
                    Vector2 v = new Vector2(i, j);

                    if (!collider.bounds.Contains(v)) continue;

                    innerPoints.Add(v);
                }
            }

            return innerPoints.ToArray();
        }

        public static Vector3Int ToVector3Int(this Vector3 vector) => new Vector3Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
        public static Vector2Int ToVector2Int(this Vector2 vector) => new Vector2Int(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));

        public static bool IsBetweenAngles(this Quaternion quaternion, Vector3 min, Vector3 max)
        {
            Vector3 clampedAngle = quaternion.eulerAngles;
            clampedAngle = NormalizeEuler(clampedAngle);

            return clampedAngle.x > min.x && clampedAngle.x < max.x
                && clampedAngle.y > min.y && clampedAngle.y < max.y
                && clampedAngle.z > min.z && clampedAngle.z < max.z;
        }

        public static Vector3 SwapXY(this Vector3 vector) => new Vector3(vector.y, vector.x);
        public static Vector3 SwapYZ(this Vector2 vector) => new Vector3(vector.x, 0, vector.y);

        public static Vector2 InvertY(this Vector2 vector) => new Vector2(vector.x, -vector.y);
        public static Vector3 NoY(this Vector3 vector) => new Vector3(vector.x, 0, vector.z);

        public static float NormalizeAngle(float angle)
        {
            float sign = Mathf.Sign(angle);

            while (Mathf.Abs(angle) > 360)
            {
                angle -= 360 * sign;
            }

            if (sign < 0) return 360f - angle;

            return angle;
        }

        public static Vector3 NormalizeEuler(Vector3 euler) => new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));

        public static Quaternion ClampRotationAroundXAxis(Quaternion q, float minAngle, float maxAngle)
        {
            var euler = q.eulerAngles;

            if (euler.x > 180)
            {
                euler.x -= 360;
            }

            euler.x = Mathf.Clamp(euler.x, minAngle, maxAngle);

            return Quaternion.Euler(euler);
        }

        public static T GetComponentInFullObject<T>(this GameObject @object) where T : Component => @object.TryGetComponent(out T t) ? t : @object.GetComponentInChildren<T>();

        public static T[] GetComponentsInFulObject<T>(this GameObject @object) where T : Component => @object.transform.root.gameObject.TryGetComponentsInChildren(out T[] ts) ? ts.Append(@object.GetComponentInRoot<T>()).ToArray() : new T[] { @object.GetComponentInRoot<T>() };

        public static T GetComponentInRoot<T>(this GameObject @object) => @object.transform.root.gameObject.GetComponent<T>();

        public static bool CompareWithLayer(this LayerMask layerMask, int layer) => layerMask.value == (layerMask.value | LayerToMask(layer));

        public static int LayerToMask(int layer) => 1 << layer;

#if PHOTON_UNITY_NETWORKING

        public static T GetPlayerProperty<T>(this Player player, string key) => (player.CustomProperties[key] is T t) ? t : default;

        public static T UpdatePlayerProperties<T>(this Player player, T dataToLoad, string key) where T : struct
        {
            Hastable playerProperties = new Hastable
            {
                { key, dataToLoad }
            };

            player.SetCustomProperties(playerProperties);
            return dataToLoad;
        }

        public static void UpdatePlayerProperties(this Player player, Hastable dataToLoad) => player.SetCustomProperties(dataToLoad);

        public static T GetRoomProperty<T>(this Room room, string key) => (room.CustomProperties[key] is T t) ? t : default;

        public static T UpdateRoomProperties<T>(this Room room, T dataToLoad, string key) where T : struct
        {
            Hastable roomProperties = new Hastable
            {
                { key, dataToLoad }
            };

            room.SetCustomProperties(roomProperties);
            return dataToLoad;
        }

        public static void UpdateRoomProperties(this Room room, Hastable dataToLoad) => room.SetCustomProperties(dataToLoad);

        public static IEnumerator PhotonLoadSceneAsync(SceneField scene)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            const float FULL_LOAD_PERCENTAGE = 0.9f;

            PhotonNetwork.LoadLevel(scene.SceneName);
            yield return new WaitUntil(() => PhotonNetwork.LevelLoadingProgress > FULL_LOAD_PERCENTAGE);
        }

#endif

        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        public static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            return comparer.Compare(hashOfInput, hash) == 0;
        }

        private delegate bool inVinVoutV<T1, T2, T3>(T1 a, T2 b, out T3 c);

        public static Vector3[] PlotForce(this Rigidbody rb, Vector3 force, Vector3 startPosition, ForceMode forceMode, int length = 120, float step = 4f, bool intersectStop = true)
        {
            step = step > 1 ? step : 1;
            length = Mathf.RoundToInt(length / step);
            List<Vector3> points = new List<Vector3>();

            Vector3 velocity = forceMode switch
            {
                ForceMode.Force => force * Time.fixedDeltaTime / rb.mass,
                ForceMode.Acceleration => force * Time.fixedDeltaTime,
                ForceMode.Impulse => force / rb.mass,
                ForceMode.VelocityChange => force,
                _ => force * Time.fixedDeltaTime / rb.mass,
            };

            inVinVoutV<Vector3, Vector3, Vector3> intersection = intersectStop ? (inVinVoutV<Vector3, Vector3, Vector3>)Intersects : (Vector3 v, Vector3 d, out Vector3 p) => { p = Vector3.zero; return false; };

            float vy = velocity.y + rb.velocity.y;
            float vx = velocity.x + rb.velocity.x;
            float vz = velocity.z + rb.velocity.z;

            float t = 0;
            float timeStep = step * Time.fixedDeltaTime;

            float DRAG_MULTIPLIER = 0.336802379f;
            float dragForce = 1 / (1 + rb.drag * timeStep * DRAG_MULTIPLIER);

            Vector3 previusPos = startPosition;

            for (int i = 0; i < length; i++)
            {
                vx *= dragForce;
                vz *= dragForce;
                vy *= dragForce;

                float dy = vy * t + 0.5f * Physics.gravity.y * Mathf.Pow(t, 2);
                float dx = vx * t;
                float dz = vz * t;

                points.Add(previusPos);
                Vector3 displacement = timeStep * new Vector3(dx, dy, dz);

                if (intersection.Invoke(previusPos, displacement, out Vector3 contactPoint))
                {
                    points.Add(contactPoint);
                    Debug.DrawLine(previusPos, contactPoint, Color.green);
                    break;
                }

                Debug.DrawLine(previusPos, previusPos + displacement, Color.green);
                previusPos += displacement;

                t += timeStep;
            }

            return points.ToArray();

            static bool Intersects(Vector3 pos, Vector3 displacement, out Vector3 hitPoint)
            {
                Ray ray = new Ray(pos, displacement);
                var r = Physics.Raycast(ray, out RaycastHit hit, displacement.magnitude);

                hitPoint = hit.point;
                return r;
            }
        }

        public static bool TryGetComponentInChildren<T>(this GameObject go, out T component) where T : Component
        {
            var comp = go.GetComponentInChildren<T>();
            component = comp;

            return comp != null;
        }

        public static bool TryGetComponentsInChildren<T>(this GameObject go, out T[] component) where T : Component
        {
            var comp = go.GetComponentsInChildren<T>();
            component = comp;

            return comp != null;
        }

#if SCENE_FIELD
        public static IEnumerator LoadSceneAsync(SceneField scene, LoadSceneParameters sceneParameters = default)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.SceneName, sceneParameters);

            yield return new WaitUntil(() => asyncLoad.isDone);
        }

        public static void LoadSceneAsync(SceneField scene, out AsyncOperation asyncOperation, LoadSceneParameters sceneParameters = default)
        {
            asyncOperation = SceneManager.LoadSceneAsync(scene.SceneName, sceneParameters);
            asyncOperation.allowSceneActivation = false;
        }
#endif

        public static Vector3 DirectionInfluencedRandomVector(Vector3 direction, float dispersionFactor)
        {
            Vector3 dispersion = Random.insideUnitSphere;
            dispersion *= Vector3.Dot(direction, dispersion) > 0 ? 1f : -1f;
            Vector3 finalDirection = Vector3.Lerp(direction, dispersion, dispersionFactor);
            return finalDirection;
        }

        public static IEnumerable<T> MaskToList<T>(Enum mask, bool reverse = false)
        {
            if (typeof(T).IsSubclassOf(typeof(Enum)) == false)
                throw new ArgumentException();

            var values = Enum.GetValues(typeof(T))
                                        .Cast<Enum>()
                                        .Where(m => mask.HasFlag(m))
                                        .Cast<T>();

            return reverse ? values.Reverse() : values;
        }

        /// <summary>Indicates whether the specified array is null or has a length of zero.</summary>
        /// <param name="array">The array to test.</param>
        /// <returns>true if the array parameter is null or has a length of zero; otherwise, false.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) => (enumerable == null || enumerable.Count() == 0);

        public static T SelectRandom<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null) return default;

            return enumerable.Count() switch
            {
                0 => default,
                1 => enumerable.FirstOrDefault(),
                _ => enumerable.ElementAtOrDefault(Random.Range(0, enumerable.Count())),
            };
        }

        public static Vector3 GlobalCenterOfMass(this Collider collider) => collider.transform.position + collider.attachedRigidbody.centerOfMass;

        public static T SelectLoopedIndex<T>(this IEnumerable<T> enumerable, int index) => enumerable.ElementAtOrDefault(index % enumerable.Count());

        public static float VectorAngleSign(Vector2 lhs, Vector2 rhs) => Mathf.Sign(Vector2.SignedAngle(lhs, rhs));

        public static int FirstSetLayer(this LayerMask mask)
        {
            int value = mask.value;
            if (value == 0) return 0;  // Early out
            for (int l = 1; l < 32; l++)
                if ((value & (1 << l)) != 0) return l;  // Bitwise
            return -1;  // This line won't ever be reached but the compiler needs it
        }

        public static IEnumerable<T> MaskToList<T>(Enum mask)
        {
            if (typeof(T).IsSubclassOf(typeof(Enum)) == false)
                throw new ArgumentException();

            return Enum.GetValues(typeof(T))
                                 .Cast<Enum>()
                                 .Where(m => mask.HasFlag(m))
                                 .Cast<T>();
        }
    
        public static float Map(float from0, float to0, float from1, float to1, float value)
        {
            float t = Mathf.InverseLerp(from0, to0, value);
            return Mathf.Lerp(from1, to1, t);
        }

        public static T[] GetInterfaces<T>(this GameObject gObj)
        {
            if (!typeof(T).IsInterface) throw new SystemException("Specified type is not an interface!");
            var mObjs = gObj.GetComponents<MonoBehaviour>();

            return (from a in mObjs where a.GetType().GetInterfaces().Any(k => k == typeof(T)) select (T)(object)a).ToArray();
        }

        public static T GetInterface<T>(this GameObject gObj)
        {
            if (!typeof(T).IsInterface) throw new SystemException("Specified type is not an interface!");
            return gObj.GetInterfaces<T>().FirstOrDefault();
        }
    }

}