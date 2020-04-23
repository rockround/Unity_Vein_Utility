using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    public Canvas canvas;
    public GameObject systemRoot;
    public Material veinMat;
    public string savePath = @"Assets\Prefabs\NewOrganSystem\";
    public string saveName = "NewVeinSystem";
    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void saveButton()
    {
        foreach(Transform parent in systemRoot.transform)
        {
            if(parent.tag == "Vein")
            {
                DestroyImmediate(parent.GetChild(0).gameObject);
            }
        }
        foreach(MeshFilter mf in systemRoot.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.tag == "Vein")
            {
                Vein v = mf.transform.GetComponent<Vein>();
                Mesh m = mf.mesh;
                string localPathM = savePath + mf.gameObject.name + ".asset";
                AssetDatabase.CreateAsset(m, localPathM);
                mf.transform.GetComponent<MeshRenderer>().material = veinMat;
            }
        }
        AssetDatabase.SaveAssets();
        string localPath = savePath + saveName + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);
        PrefabUtility.SaveAsPrefabAssetAndConnect(systemRoot, localPath, InteractionMode.UserAction);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            canvas.enabled = !canvas.enabled;
        }
    }
}
