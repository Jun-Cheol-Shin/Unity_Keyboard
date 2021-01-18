using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardManager : MonoBehaviour
{

    private static KeyboardManager instance;

    public enum KeyBoardType
    {
        NONE = 0,
        LowerEng = 1,       // 소문자
        UpperEng,           // 대문자
        Kor1,               // 한글
        Kor2,               // 한글 쌍자음
        Special1,           // 특문 1페이지
        Special2,           // 특문 2페이지
    }

    // 캔버스 변수
    GameObject mainCanvasObj;
    Canvas mainCanvas;
    RectTransform rectTransform;

    RectTransform panelRect;

    //GameObject buttonObj;


    public Vector2 resolution;
    public float spacebarSize = 500;
    public float enterSize = 300;
    public float shiftSize = 100;
    public float backspaceSize = 100;


    public KeyBoardType preType;
    public KeyBoardType type = KeyBoardType.LowerEng;


    [HideInInspector]
    public GameObject text;
    [HideInInspector]
    public Text t;


    GameObject field;


    [SerializeField]
    public Stack<char> fieldlist = new Stack<char>();

    readonly List<Key> EngbuttonList = new List<Key>();
    readonly List<Key> KorbuttonList = new List<Key>();
    readonly List<Key> SpecialbuttonList = new List<Key>();

    // 키보드 형식
    const string EngkeyOrder = "1|2|3|4|5|6|7|8|9|0|q|w|e|r|t|y|u|i|o|p|a|s|d|f|g|h|j|k|l|shift|z|x|c|v|b|n|m|backspace|123|한/영|,|space|.|Enter";
    const string KorKeyOrder = "1|2|3|4|5|6|7|8|9|0|ㅂ|ㅈ|ㄷ|ㄱ|ㅅ|ㅛ|ㅕ|ㅑ|ㅐ|ㅔ|ㅁ|ㄴ|ㅇ|ㄹ|ㅎ|ㅗ|ㅓ|ㅏ|ㅣ|shift|ㅋ|ㅌ|ㅊ|ㅍ|ㅠ|ㅜ|ㅡ|backspace|123|한/영|,|space|.|Enter";
    const string special1Order = "1|2|3|4|5|6|7|8|9|0|＋|×|÷|＝|/|_|<|>|♡|☆|!|@|#|~|%|^|&|*|(|)|next|-|'|\"|:|;|,|?|backspace|123|한/영|,|space|.|Enter";


    const string special1 = "＋|×|÷|＝|/|_|<|>|♡|☆|!|@|#|~|%|^|&|*|(|)|-|'|\"|:|;|,|?";
    const string special2 = "`^￦^\\^|^♤^♧^{^}^[^]^○^●^□^■^◇^$^€^￡^￥^º^※^⊙^≪^≫^¡^¿^☏";


    GameObject kor;
    GameObject eng;
    GameObject spe;
    

    public char fieldReturn()
    {
        if(fieldlist.Count > 0)
        {
            return fieldlist.Peek();
        }

        else
        {
            return ' ';
        }
    }
    public void fieldPush(char key)
    {
        fieldlist.Push(key);
        t.text += key;
    }
    public void fieldPop()
    {
        if(t.text.Length > 0)
        {
            //Debug.LogFormat("{0}", t.text.Length - 1);
            string str = t.text.Substring(0, t.text.Length - 1);
            t.text = str;
            char temp = fieldlist.Pop();
        }
    }

    private void Start()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            //만약 씬 이동이 되었는데 그 씬에도 Hierarchy에 GameMgr이 존재할 수도 있다.
            //그럴 경우엔 이전 씬에서 사용하던 인스턴스를 계속 사용해주는 경우가 많은 것 같다.
            //그래서 이미 전역변수인 instance에 인스턴스가 존재한다면 자신(새로운 씬의 GameMgr)을 삭제해준다.
            Destroy(this.gameObject);
        }

        Initialized();
    }

    public static KeyboardManager Instance
    {
        get
        {
            if(null == instance)
            {
                return null;
            }
            return instance;
        }
    }

    // 영문 키보드 or 한글 키보드 제작 게임 오브젝트 생성 및 정렬
    GameObject MakeKeyBoard(KeyBoardType type, bool set)
    {

        // 패널 생성
        GameObject panel = new GameObject(type.ToString());
        panel.layer = LayerMask.NameToLayer("UI");
        panel.AddComponent<CanvasRenderer>();
        Image image = panel.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        image.type = Image.Type.Sliced;
        //image.sprite = Resources.GetBuiltinResource(typeof(Sprite), "Background.psd") as Sprite;
        //i.sprite = Resources.Load("Assets/Background") as Sprite;

        panel.transform.SetParent(mainCanvasObj.transform, false);
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector3(resolution.x * 1.05f, resolution.x * 0.4f);

        panelRect.anchoredPosition3D = new Vector3(0, resolution.x * 1.5f, resolution.x);
        panelRect.localRotation = Quaternion.Euler(35f, 0f, 0f);

        // 세로로 정렬할 부모 오브젝트 생성
        GameObject vertical = new GameObject("VerticalLayer");
        vertical.transform.SetParent(panel.transform);
        VerticalLayoutGroup vV = vertical.AddComponent<VerticalLayoutGroup>();
        vV.spacing = 30f;
        RectTransform vR = vertical.GetComponent<RectTransform>();
        vR.localScale = new Vector3(1, 1, 1);
        vR.sizeDelta = new Vector3(resolution.x, resolution.x * 0.3f);
        vR.localRotation = Quaternion.Euler(Vector3.zero);
        vR.anchoredPosition3D = Vector3.zero;

        // 버튼 오브젝트
        GameObject buttonObj = Resources.Load("Button") as GameObject;

        // 버튼 리스트
        List<Key> buttonList;
        string[] parse;

        if(type == KeyBoardType.LowerEng)
        {
            parse = EngkeyOrder.Split('|');
            buttonList = EngbuttonList;
        }

        else if(type == KeyBoardType.Special1)
        {
            parse = special1Order.Split('|');
            buttonList = SpecialbuttonList;
        }

        else
        {
            parse = KorKeyOrder.Split('|');
            buttonList = KorbuttonList;

        }

        int c = 0;

        // 수평으로 정렬할 빈 오브젝트 5개 생성
        GameObject[] a = new GameObject[5];

        // 1줄에 버튼 여러개 생성
        for(int i = 0; i < 5; i++)
        {
            a[i] = new GameObject("HorizotalLayer");
            HorizontalLayoutGroup aH = a[i].AddComponent<HorizontalLayoutGroup>();
            a[i].transform.SetParent(vertical.transform);
            RectTransform aR = a[i].GetComponent<RectTransform>();
            aR.localScale = new Vector3(1, 1, 1);
            aR.localRotation = Quaternion.Euler(Vector3.zero);
            aR.anchoredPosition3D = Vector3.zero;

            switch(i)
            {
                case 0:
                case 1:
                for(int j = 0; j < 10; j++)
                {
                    GameObject button = Instantiate(buttonObj, a[i].transform);
                    button.transform.GetChild(0).GetComponent<Text>().text = parse[c++];
                    button.transform.GetChild(0).GetComponent<Text>().fontSize = 50;
                    Key k = button.AddComponent<Key>();
                    button.layer = LayerMask.NameToLayer("UI");
                    buttonList.Add(k);
                }
                aH.spacing = 40f;
                break;

                case 2:
                case 3:
                int index = 9;
                if(type == KeyBoardType.Special1 && i == 2)
                    index = 10;

                for(int j = 0; j < index; j++)
                {
                    GameObject button = Instantiate(buttonObj, a[i].transform);
                    if(parse[c] == "shift")
                    {
                        LayoutElement layout = button.AddComponent<LayoutElement>();
                        layout.preferredWidth = shiftSize;
                    }
                    else if(parse[c] == "backspace")
                    {
                        LayoutElement layout = button.AddComponent<LayoutElement>();
                        layout.preferredWidth = backspaceSize;
                    }
                    button.transform.GetChild(0).GetComponent<Text>().text = parse[c++];
                    button.transform.GetChild(0).GetComponent<Text>().fontSize = 50;
                    Key k = button.AddComponent<Key>();
                    button.layer = LayerMask.NameToLayer("UI");
                    buttonList.Add(k);
                }
                aH.spacing = 40f;
                break;

                case 4:
                for(int j = 0; j < 6; j++)
                {
                    GameObject button = Instantiate(buttonObj, a[i].transform);
                    if(parse[c] == "space")
                    {
                        LayoutElement layout = button.AddComponent<LayoutElement>();
                        layout.preferredWidth = spacebarSize;
                    }
                    else if(parse[c] == "Enter")
                    {
                        LayoutElement layout = button.AddComponent<LayoutElement>();
                        layout.preferredWidth = enterSize;
                    }

                    button.transform.GetChild(0).GetComponent<Text>().text = parse[c++];
                    button.transform.GetChild(0).GetComponent<Text>().fontSize = 50;
                    Key k = button.AddComponent<Key>();
                    button.layer = LayerMask.NameToLayer("UI");
                    buttonList.Add(k);
                }
                aH.spacing = 40f;
                break;
            }
        }

        panel.SetActive(set);


        return panel;
    }


    // 캔버스, 패널, 버튼 등 게임오브젝트 생성.
    void Initialized()
    {

        // 캔버스 만들기
        mainCanvasObj = new GameObject("Canvas");

        rectTransform = mainCanvasObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = resolution;
        rectTransform.localScale = new Vector3(1f / resolution.x, 1f / resolution.x, 1f / resolution.x);

        mainCanvas = mainCanvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.WorldSpace;

        mainCanvasObj.AddComponent<CanvasScaler>();
        mainCanvasObj.AddComponent<GraphicRaycaster>();

        mainCanvasObj.layer = LayerMask.NameToLayer("UI");


        // 키보드 2개 미리 제작
        kor = MakeKeyBoard(KeyBoardType.Kor1, false);
        eng = MakeKeyBoard(KeyBoardType.LowerEng, true);
        spe = MakeKeyBoard(KeyBoardType.Special1, false);


        // 텍스트 필드 객체 만들기
        field = new GameObject("TextField");
        RectTransform fR = field.AddComponent<RectTransform>();
        field.AddComponent<CanvasRenderer>();
        field.AddComponent<Image>();
        field.transform.SetParent(mainCanvasObj.transform);
        fR.localScale = new Vector3(1, 1, 1);
        fR.anchoredPosition3D = new Vector3(0, 3500, 2350);
        fR.sizeDelta = new Vector2(1000, 300);
        fR.rotation = Quaternion.Euler(new Vector3(35, 0, 0));
        field.layer = LayerMask.NameToLayer("UI");

        // 텍스트 객체 생성
        text = new GameObject("Text");
        RectTransform tR = text.AddComponent<RectTransform>();
        text.AddComponent<CanvasRenderer>();
        t = text.AddComponent<Text>();
        text.transform.SetParent(field.transform);
        text.layer = LayerMask.NameToLayer("UI");

        tR.anchoredPosition3D = new Vector3(0, 0, 0);
        tR.sizeDelta = fR.sizeDelta;
        tR.localRotation = Quaternion.Euler(Vector3.zero);
        tR.localScale = new Vector3(1, 1, 1);
        t.fontSize = 50;
        t.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
        t.color = Color.black;
    }


    // shift 키
    public void ShiftKeyBoard()
    {
        if(type == KeyBoardType.UpperEng)
        {
            type = KeyBoardType.LowerEng;
            foreach(Key item in EngbuttonList)
            {
                item.Change();
            }
        }

        else if(type == KeyBoardType.LowerEng)
        {
            type = KeyBoardType.UpperEng;
            foreach(Key item in EngbuttonList)
            {
                item.Change();
            }
        }

        else if(type == KeyBoardType.Kor1)
        {
            type = KeyBoardType.Kor2;
            foreach(Key item in KorbuttonList)
            {
                item.Change();
            }
        }

        else if( type == KeyBoardType.Kor2)
        {
            type = KeyBoardType.Kor1;
            foreach(Key item in KorbuttonList)
            {
                item.Change();
            }
        }

    }
    // 한/영 키
    public void KorEngChange(KeyBoardType type)
    {

        kor.SetActive(false);
        eng.SetActive(false);
        spe.SetActive(false);

        switch(type)
        {
            case KeyBoardType.Kor1:
            kor.SetActive(true);
            foreach(Key item in KorbuttonList)
            {
                item.Change();
            }
            this.type = type;
            break;

            case KeyBoardType.LowerEng:
            eng.SetActive(true);
            foreach(Key item in EngbuttonList)
            {
                item.Change();
            }
            this.type = type;
            break;

            case KeyBoardType.Special1:
            spe.SetActive(true);
            preType = this.type;
            this.type = type;
            break;
        }
    }


    // 특문 1~2페이지로 넘어가는 함수
    public void SpecialShift()
    {

        if(type == KeyBoardType.Special1)
        {
            type = KeyBoardType.Special2;
        }

        else
        {
            type = KeyBoardType.Special1;
        }

        string[] str;
        int count = 0;

        switch(type)
        {
            case KeyBoardType.Special1:
            str = special1.Split('|');

            for(int i = 0; i < SpecialbuttonList.Count; i++)
            {
                if(SpecialbuttonList[i].keyType == Key.Type.SPECIAL && SpecialbuttonList[i].keyname.Length == 1)
                {
                    SpecialbuttonList[i].text.text = str[count];
                    SpecialbuttonList[i].keyname = str[count++].ToCharArray();
                    SpecialbuttonList[i].keyvalue = SpecialbuttonList[i].keyname[0];

                    if(count == str.Length)
                    {
                        break;
                    }
                }
            }
            break;

            case KeyBoardType.Special2:
            str = special2.Split('^');
     
            for(int i = 0; i < SpecialbuttonList.Count; i++)
            {
                if(SpecialbuttonList[i].keyType == Key.Type.SPECIAL && SpecialbuttonList[i].keyname.Length == 1)
                {
                    SpecialbuttonList[i].text.text = str[count];
                    SpecialbuttonList[i].keyname = str[count++].ToCharArray();
                    SpecialbuttonList[i].keyvalue = SpecialbuttonList[i].keyname[0];

                    if(count == str.Length)
                    {
                        break;
                    }
                }
            }
            break;
        }
    }
}
