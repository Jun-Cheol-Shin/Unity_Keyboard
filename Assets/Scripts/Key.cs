using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class Key : MonoBehaviour
{
    public enum Type
    {
        LOWER = 1,                      // 영어 소문자
        UPPER,                          // 영어 대문자
        CONSONANT,                      // 한글 자음
        VOWEL,                          // 모음
        FUNCTION,                       // 기능 (엔터, 쉬프트 등)
        SPECIAL,                        // 특수문자 (쉼표 등)
        NUMBER,                         // 숫자
    }

    [SerializeField]
    public char[] keyname;
    public char keyvalue;

    Button myButton;

    public Type keyType;
    //public int Code;

    public Text text;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(Click);
        text = transform.GetChild(0).GetComponent<Text>();

        Classify();
    }


    // 처음 키보드가 생성 후 키들을 분류한다. type에 따라 분류한다.
    void Classify()
    {
        keyname = text.text.ToCharArray();
        if(keyname.Length == 1)
        { 
            // 숫자
            if(keyname[0] >= 48 && keyname[0] <= 57)
            {
                keyType = Type.NUMBER;
            }

            // 소문자 영어
            else if(keyname[0] >= 97 && keyname[0] <= 122)
            {
                keyType = Type.LOWER;
            }

            // 한글 자음 모음 분류
            else if(char.GetUnicodeCategory(keyname[0]).ToString() == "OtherLetter")
            {
                if(keyname[0] >= '\x3130' && keyname[0] < '\x314F')
                {
                    keyType = Type.CONSONANT;
                }   

                else if(keyname[0] >= '\x314F' && keyname[0] < '\x318F')
                {
                    keyType = Type.VOWEL;
                }
            }

            // 그외는 특수문자 . , 
            else
            {
                keyType = Type.SPECIAL;
            }

            //Code = keyname[0];
            keyvalue = keyname[0];
        }

        // 펑션키 지정
        else
        { 
            switch(text.text.ToUpper())
            {
                case "SPACE":
                keyvalue = ' ';
                break;

                case "ENTER":
                keyvalue = '\n';
                break;

                case "BACKSPACE":
                keyvalue = '\b';
                break;

                case "NEXT":
                case "SHIFT":
                keyvalue = '㉾';
                break;

                case "한/영":
                keyvalue = 'ª';
                break;

                case "123":
                keyvalue = '®';
                break;
            }
            keyType = Type.FUNCTION;
        }
    }


    // 내가 정해준 value값에 따라 다른 함수를 실행시키도록 한다.
    public void Click()
    {
        switch(KeyboardManager.Instance.type)
        {
            // 영어는 간단하므로 입력에 따라 field 스택에 push해준다.
            case KeyboardManager.KeyBoardType.UpperEng:
            case KeyboardManager.KeyBoardType.LowerEng:
            switch(keyvalue)
            {
                // 백스페이스
                case '\b':
                KeyboardManager.Instance.fieldPop();
                break;

                // 쉬프트
                case '㉾':
                KeyboardManager.Instance.ShiftKeyBoard();
                break;

                // 한영 전환
                case 'ª':
                KeyboardManager.Instance.KorEngChange(KeyboardManager.KeyBoardType.Kor1);
                break;

                // 특문키
                case '®':
                KeyboardManager.Instance.KorEngChange(KeyboardManager.KeyBoardType.Special1);
                break;

                default:
                KeyboardManager.Instance.fieldPush(keyvalue);
                break;
            }
            break;

            // 한글이 어렵다
            case KeyboardManager.KeyBoardType.Kor1:
            case KeyboardManager.KeyBoardType.Kor2:
            switch(keyvalue)
            {   
                // 백스페이스
                case '\b':
                KeyboardManager.Instance.fieldPop();
                break;

                // 쉬프트
                case '㉾':
                KeyboardManager.Instance.ShiftKeyBoard();
                break;

                // 한영 전환
                case 'ª':
                KeyboardManager.Instance.KorEngChange(KeyboardManager.KeyBoardType.LowerEng);
                break;

                // 특문키
                case '®':
                KeyboardManager.Instance.KorEngChange(KeyboardManager.KeyBoardType.Special1);
                break;

                default:
                // 일단 한글의 자음, 모음만 이 함수를 실행시킨다.
                if(keyType == Type.CONSONANT || keyType == Type.VOWEL)
                {
                    KorMergeSepaMethod();
                }

                // 특문은 push
                else
                {
                    KeyboardManager.Instance.fieldPush(keyvalue);
                }
                break;
            }
            break;

            case KeyboardManager.KeyBoardType.Special1:
            case KeyboardManager.KeyBoardType.Special2:
            switch(keyvalue)
            {
                // 백스페이스
                case '\b':
                KeyboardManager.Instance.fieldPop();
                break;

                // 특문 쉬프트
                case '㉾':
                KeyboardManager.Instance.SpecialShift();
                break;

                // 한영전환
                case 'ª':
                KeyboardManager.Instance.KorEngChange(KeyboardManager.Instance.preType);
                break;

                // 특문 키 => 한영키
                case '®':
                KeyboardManager.Instance.KorEngChange(KeyboardManager.Instance.preType);
                break;

                default:
                KeyboardManager.Instance.fieldPush(keyvalue);
                break;
            }
            break;
        }
    }


    // 핵심 함수
    public void KorMergeSepaMethod()
    {
        // c 는 스택의 마지막 값 => 가장 마지막에 입력한 문자를 스택에서 peek() 한 값
        char c = KeyboardManager.Instance.fieldReturn();
        // c 가 조립된 상태 ex)=> 산 => ㅅ , ㅏ , ㄱ 으로 char 배열 3개를 리턴 받는다.
        char[] retval = JamoManager.Instance.JamoSeparate(c);

        // 자모매니저에서 받아온 타입에 따라 다르게 실행.
        switch(JamoManager.Instance.Return(c, keyvalue))
        {
            // none은 자음이나 모음이 연속으로 올때 즉 조립이 불가 혹은 아무것도 없는 상태 단순하게 push하여 새로 문자 생성.
            case JamoManager.Hangul.NONE:
            KeyboardManager.Instance.fieldPush(keyvalue);
            break;

            // complete는 초,중,종성있고 종성이 쌍자음까지 있는 경우를 뜻함.
            case JamoManager.Hangul.COMPLETE:
            if(keyType == Type.VOWEL)
            {
                char[] separate = JamoManager.Instance.SeparateConsonant(retval[2]);

                Debug.Log("종성 완료 => 앉 다음으로 ㅣ를 치면  => 안지");
                KeyboardManager.Instance.fieldPop();
                char merge = JamoManager.Instance.JamoMerge(retval[0], retval[1], separate[0]);
                KeyboardManager.Instance.fieldPush(merge);

                merge = JamoManager.Instance.JamoMerge(separate[1], keyvalue, ' ');
                KeyboardManager.Instance.fieldPush(merge);
            }

            else
            {
                KeyboardManager.Instance.fieldPush(keyvalue);
            }
            break;


            // 자모는 애매하게 초성,중성 종성(쌍자음x)만 있거나 초성만 있는 경우 혹은 중성만 있는 경우 초성, 중성 섞인 경우
            case JamoManager.Hangul.JAMO:

            // 마지막 입력값이 자음만 있을 경우 ex) => ㄱ, ㄴ
            if(c >= '\x3130' && c < '\x314F')
            {
                // 자음에 자음
                if(keyType == Type.CONSONANT)
                {
                    Debug.Log("자음에 자음 => ㄱ -> ㄱ");
                    KeyboardManager.Instance.fieldPush(keyvalue);
                }

                // 자음에 모음
                else
                {
                    Debug.Log("자음에 모음 => ㄱ -> ㅏ");
                    char merge = JamoManager.Instance.JamoMerge(c, keyvalue, ' ');
                    KeyboardManager.Instance.fieldPop();
                    KeyboardManager.Instance.fieldPush(merge);
                }
            }

            // 마지막 입력값이 모음만 있는 경우
            else if(c >= '\x314F' &&c < '\x318F')
            {
                // 모음에 자음
                if(keyType == Type.CONSONANT)
                {
                    Debug.Log("모음에 자음 => ㅣ -> ㄱ");
                    KeyboardManager.Instance.fieldPush(keyvalue);
                }

                else
                {
                    Debug.Log("모음에 모음 => ㅗ -> ㅣ => ㅚ");
                    char r = JamoManager.Instance.CheckVowel(c, keyvalue);
                    KeyboardManager.Instance.fieldPop();
                    KeyboardManager.Instance.fieldPush(r);
                }
            }

            // 초성, 중성이 섞인 경우
            else
            {
                // 종성이 없을 때
                if(retval[2] == ' ')
                {
                    // 자음이 들어와서 종성이 되느냐
                    if(keyType == Type.CONSONANT)
                    {
                        Debug.Log("자음이 들어와 종성까지 -> ㄱ ㅏ ㄱ => 각");
                        char merge = JamoManager.Instance.JamoMerge(retval[0], retval[1], keyvalue);
                        KeyboardManager.Instance.fieldPop();
                        KeyboardManager.Instance.fieldPush(merge);
                    }

                    // 모음이 한번 더 들어가서 ㅘ ㅢ 같은 경우가 되느냐
                    else
                    {
                        Debug.Log("ㅘ ㅢ 만들어짐");
                        char asem = JamoManager.Instance.CheckVowel(retval[1], keyvalue);
                        char merge = JamoManager.Instance.JamoMerge(retval[0], asem, ' ');
                        KeyboardManager.Instance.fieldPop();
                        KeyboardManager.Instance.fieldPush(merge);
                    }
                }
                

                // 종성이 있으나 쌍자음이 아닌 경우
                else
                {
                    // 자음이 들어와서 종성을 쌍자음으로 만드느냐.
                    if(keyType == Type.CONSONANT)
                    {
                        Debug.Log("ㄶ ㅀ 만들어짐");
                        char asem = JamoManager.Instance.CheckConsonant(retval[2], keyvalue);
                        char merge = JamoManager.Instance.JamoMerge(retval[0], retval[1], asem);
                        KeyboardManager.Instance.fieldPop();
                        KeyboardManager.Instance.fieldPush(merge);
                    }

                    // 종성을 하나의 자음으로 끝내고 모음으로 다른 문자를 만드느냐
                    else
                    {
                        Debug.Log("종성 미완료 => 안 다음으로 ㅣ를 치면  => 아니");
                        KeyboardManager.Instance.fieldPop();
                        char merge = JamoManager.Instance.JamoMerge(retval[0], retval[1], ' ');
                        KeyboardManager.Instance.fieldPush(merge);

                        merge = JamoManager.Instance.JamoMerge(retval[2], keyvalue, ' ');
                        KeyboardManager.Instance.fieldPush(merge);
                    }
                }
            }
            break;
        }

    }


    // shift를 눌렀을 경우.
    public void Change()
    {
        switch(KeyboardManager.Instance.type)
        {
            case KeyboardManager.KeyBoardType.LowerEng:
            if(keyType == Type.UPPER)
            {
                text.text = text.text.ToLower();
                keyname = text.text.ToCharArray();
                keyvalue = keyname[0];
                keyType = Type.LOWER;
            }
            break;

            case KeyboardManager.KeyBoardType.UpperEng:
            if(keyType == Type.LOWER)
            {
                text.text = text.text.ToUpper();
                keyname = text.text.ToCharArray();
                keyvalue = keyname[0];
                keyType = Type.UPPER;
            }
            break;

            case KeyboardManager.KeyBoardType.Kor2:
            switch(keyname[0])
            {
                case 'ㄱ':
                case 'ㅂ':
                case 'ㅈ':
                case 'ㄷ':
                case 'ㅅ':
                for(int i=0; i<JamoManager.Instance.m_ChoSungTbl.Length; i++)
                {
                    if(JamoManager.Instance.m_ChoSungTbl[i].CompareTo(keyname[0]) == 0)
                    {
                        keyname[0] = JamoManager.Instance.m_ChoSungTbl[i + 1];
                        text.text = keyname[0].ToString();
                        keyvalue = keyname[0];
                        break;
                    }
                }
                break;

                case 'ㅐ':
                case 'ㅔ':
                for(int i = 0; i < JamoManager.Instance.m_JungSungTbl.Length; i++)
                {
                    if(JamoManager.Instance.m_JungSungTbl[i].CompareTo(keyname[0]) == 0)
                    {
                        keyname[0] = JamoManager.Instance.m_JungSungTbl[i + 2];
                        text.text = keyname[0].ToString();
                        keyvalue = keyname[0];
                        break;
                    }
                }
                break;
            }
            break;

            case KeyboardManager.KeyBoardType.Kor1:
            switch(keyname[0])
            {
                case 'ㄲ':
                case 'ㅃ':
                case 'ㅉ':
                case 'ㄸ':
                case 'ㅆ':
                for(int i = 0; i < JamoManager.Instance.m_ChoSungTbl.Length; i++)
                {
                    if(JamoManager.Instance.m_ChoSungTbl[i].CompareTo(keyname[0]) == 0)
                    {
                        keyname[0] = JamoManager.Instance.m_ChoSungTbl[i - 1];
                        text.text = keyname[0].ToString();
                        keyvalue = keyname[0];
                        break;
                    }
                }
                break;

                case 'ㅒ':
                case 'ㅖ':
                for(int i = 0; i < JamoManager.Instance.m_JungSungTbl.Length; i++)
                {
                    if(JamoManager.Instance.m_JungSungTbl[i].CompareTo(keyname[0]) == 0)
                    {
                        keyname[0] = JamoManager.Instance.m_JungSungTbl[i - 2];
                        text.text = keyname[0].ToString();
                        keyvalue = keyname[0];
                        break;
                    }
                }
                break;
            }
            break;

            case KeyboardManager.KeyBoardType.Special1:

            break;

            case KeyboardManager.KeyBoardType.Special2:

            break;
        }
    }
}
