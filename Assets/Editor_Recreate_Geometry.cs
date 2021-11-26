#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace Myy
{
    public class MapLoader : EditorWindow
    {
        const string buttonText = "Read photo data";
        SerializedObject serialO;

        public GameObject rootObject;
        SerializedProperty rootObjectSerialized;

        public Mesh[] floorLibrary;
        SerializedProperty floorLibrarySerialized;
        public Mesh[] wallsLibrary;
        SerializedProperty wallsLibrarySerialized;

        public string photoOrScreenshotPath = "";
        SerializedProperty photoOrScreenshotPathSerialized;
        public MeshFilter wallsMeshFilter;
        SerializedProperty wallsMeshFilterSerialized;
        public MeshFilter floorMeshFilter;
        SerializedProperty floorMeshFilterSerialized;


        private void OnEnable()
        {
            serialO = new SerializedObject(this);
            rootObjectSerialized = serialO.FindProperty("rootObject");
            floorLibrarySerialized = serialO.FindProperty("floorLibrary");
            wallsLibrarySerialized = serialO.FindProperty("wallsLibrary");
            photoOrScreenshotPathSerialized = serialO.FindProperty("photoOrScreenshotPath");

            wallsMeshFilterSerialized = serialO.FindProperty("wallsMeshFilter");
            floorMeshFilterSerialized = serialO.FindProperty("floorMeshFilter");

        }

        [MenuItem("Voyage / Load Geometry from picture")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MapLoader), false, "Texture Array Generator");
        }

        private int Color32ToInt(Color32 color)
        {
            int returnedInt = 0 | 
                ((color.b << 16) |
                 (color.g <<  8) |
                 (color.r <<  0));
            Debug.Log($"Color : {color} -> {returnedInt}");
            return returnedInt;
        }        

        private int[] Color32ArrayToIntArray(Color32[] colors, int from, int n_values)
        {
            int[] intArray = new int[n_values];

            for (int intI = 0, c = from; intI < n_values && c < from+n_values; c++, intI++)
            {
                intArray[intI] = Color32ToInt(colors[c]);
            }

            return intArray;
        }

        /* This is only used to apply the right texture, using
         * a Texture2DArray shader.
         * Feel free to drop this if you want to apply the
         * texture with a single material, using multiple material
         * slots (the standard way of doing things in Unity)
         */
        private Mesh SetTextureInfoOnMesh(Mesh meshToSetup, int textureId)
        {
            Color32[] colors = new Color32[meshToSetup.vertices.Length];
            int n_colors = colors.Length;

            Color32 applied_color = new Color32((byte) textureId, 0, 0, 255);
            for (int i = 0; i < n_colors; i++)
            {
                colors[i] = applied_color;
            }

            meshToSetup.colors32 = colors;
            return meshToSetup;

        }

        void RegenerateStratum(
            string name,
            Color32[] stratumData,
            int offsetInData,
            Mesh[] library,
            MeshFilter setTo,
            int nTiles /* FIXME: This argument MUST be included in the data */)
        {
                CombineInstance[] tilesCombination = new CombineInstance[64];
                Vector3 position = new Vector3();

                Quaternion rotation = Quaternion.identity;
                for (int pixel = offsetInData, tileIndex = 0; pixel < offsetInData+nTiles; pixel++, tileIndex++)
                {
                    Color32 currentTileData = stratumData[pixel];
                    int meshIndex    = currentTileData.r;
                    int textureIndex = currentTileData.g;
                    int orientation  = currentTileData.b;

                    position.x = (tileIndex & 7);
                    position.z = ((tileIndex / 8) & 7);

                    rotation = Quaternion.Euler(0, orientation * 45, 0);

                    CombineInstance tile = tilesCombination[tileIndex];
                    tile.mesh         = SetTextureInfoOnMesh(Instantiate(library[meshIndex]), textureIndex);
                    tile.transform    = Matrix4x4.Translate(position) * Matrix4x4.Rotate(rotation);
                    tilesCombination[tileIndex] = tile;
                }

                Mesh stratum = new Mesh();
                stratum.CombineMeshes(tilesCombination);

                AssetDatabase.CreateAsset(stratum, $"Assets/{name}.mesh");
                setTo.sharedMesh = stratum;

        }

        private void OnGUI()
        {
            bool everythingOK = true;
            serialO.Update();

            EditorGUILayout.PropertyField(rootObjectSerialized);
            if (rootObject == null)
            {
                everythingOK = false;
            }

            EditorGUILayout.PropertyField(floorLibrarySerialized);
            if (floorLibrary == null || floorLibrary.Length == 0)
            {
                everythingOK = false;
            }

            EditorGUILayout.PropertyField(wallsLibrarySerialized);
            if (wallsLibrary == null || wallsLibrary.Length == 0)
            {
                everythingOK = false;
            }

            EditorGUILayout.PropertyField(floorMeshFilterSerialized);
            if (floorMeshFilter == null)
            {
                everythingOK = false;
            }

            EditorGUILayout.PropertyField(wallsMeshFilterSerialized);
            if (wallsMeshFilter == null)
            {
                everythingOK = false;
            }

            EditorGUILayout.PropertyField(photoOrScreenshotPathSerialized);
            if (GUILayout.Button("Select photo path"))
            {
                photoOrScreenshotPath = EditorUtility.OpenFilePanel("Select data photo", "", "png");
            }
            if (photoOrScreenshotPath == null)
            {
                everythingOK = false;
            }

            serialO.ApplyModifiedProperties();

            if (!everythingOK) return;

            if (GUILayout.Button(buttonText))
            {
                byte[] bytes = File.ReadAllBytes(photoOrScreenshotPath);
                Texture2D texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.LoadImage(bytes);
                Color32[] data = texture.GetPixels32(0);
                Color32 sig1 = data[0];
                Color32 sig2 = data[1];
                byte[] signature = {sig1.r, sig1.g, sig1.b, sig2.r, sig2.g, sig2.b};
                Debug.Log($"{Encoding.UTF8.GetString(signature)}");


                Mesh emptyMesh = new Mesh();
                floorLibrary[0] = emptyMesh;
                wallsLibrary[0] = emptyMesh;

                /* FIXME : Fix the magic values '64'. These are the number of tiles
                 * for each 'stratum' (floor, walls, ...), but these number need to
                 * be encoded in the file and retrieved from the file, instead of
                 * being guessed.
                 */
                RegenerateStratum("floor", data, 1920*4, floorLibrary, floorMeshFilter, 64);
                RegenerateStratum("walls", data, (1920*4)+64, wallsLibrary, wallsMeshFilter, 64);

                
            }
        }


    }
}
#endif