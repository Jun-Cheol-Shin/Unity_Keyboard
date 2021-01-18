using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;

public class JamoManager : MonoBehaviour
{
    public enum Hangul
    {
        NONE = 0,
        COMPLETE = 1,
        JAMO,
        CHOSUNG,
        JUNGSUNG,
        JONGSUNG,
    }

    readonly public string m_ChoSungTbl =

    "ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ";

    readonly public string m_JungSungTbl =

    "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";

    readonly public string m_JongSungTbl =

    " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

    private const ushort m_UniCodeHangulBase = 0xAC00;
    private const ushort m_UniCodeHangulLast = 0xD79F;

    private static JamoManager instance;

    void Start()
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
    }

    public static JamoManager Instance
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


    public Hangul Return(char c, char current)
    {
        // 아무것도 없는 경우
        if(c == ' ')
        {
            return Hangul.NONE;
        }

        // 자음 하나만 있는 경우
        else if(c >= '\x3130' && c < '\x314F')
        {
            return Hangul.JAMO;
        }

        // 모음 하나만 있는 경우

        else if(c >= '\x314F' && c < '\x318F')
        {
            // 자음이 조립이 되는지 확인 후..
            char retval = CheckVowel(c, current);

            // 조립이 안된다!
            if(retval.CompareTo(c) == 0)
            {
                return Hangul.NONE;
            }

            // 조립이 된다.
            else
            {
                return Hangul.JAMO;
            }
        }


        // 초성 중성 섞였을 경우..
        else if(c >= '\xAC00' && c <= '\xD7AF')
        {
            // 초성, 중성, 종성이 모두 있고 종성에서 쌍자음으로 만들어질 수 있을 경우
            if(CheckJamo(c, current))
            {
                // 모음 이라면..
                if(current >= '\x314F' && current < '\x318F')
                {
                    char[] retval = JamoSeparate(c);
                    char[] sepa = SeparateConsonant(retval[2]);

                    if(sepa[1] == ' ')          // 종성이 쌍자음이 아닐때 ex) 잇 상태에서 => 'ㅅ' 나 'ㅈ'를 쳤을 경우
                    {
                        return Hangul.JAMO;
                    }

                    else                        // 종성이 쌍자음일 경우 ex) '있' 상태에서 => 'ㅅ'를 쳤을 경우 '있ㅅ'
                    {
                        return Hangul.COMPLETE;
                    }
                }
                
                // 자음 이라면..
                else
                {
                    return Hangul.COMPLETE;
                }
            }

            // 초성 혹은 초성, 중성만 있는 경우
            else
            {
                // 종성 리스트에서 입력값과 같은 것을 찾아냈을 경우.. => 결과값은 초성 , 중성, 종성으로 완성
                for(int i=0; i < m_JongSungTbl.Length; i++)
                {
                    if(m_JongSungTbl[i].CompareTo(current) == 0)
                    {
                        return Hangul.JAMO;
                    }
                }

                // 모음을 받아서 모음이 조립이 되는지 판단       => 결과값은 으 상태에서 ㅣ를 쳤을 경우 => 의
                char[] s = JamoSeparate(c);
                char retval = CheckVowel(s[1], current);

                if(s[1] != retval)                          // 조립 가능
                {
                    return Hangul.JAMO;
                }
                else                                        // 조립 불가
                {
                    return Hangul.NONE;
                }

                //return Hangul.COMPLETE;

            }
        }

        //// 조립 가능
        //else if(c >= '\x3130' && c < '\x314F')
        //{
        //    return Hangul.CHOSUNG;
        //}
        //// 조립 가능
        //else if(c >= '\x314F' && c < '\x318F')
        //{
        //    return Hangul.JUNGSUNG;
        //}

        // 그외의 경우에는 조립 불가능
        return Hangul.NONE;
    }

    // 초성 중성, 종성을 받아 하나의 문자로 조립하는 함수
    public char JamoMerge(char chosung, char jungsung, char jongsung)
    {

        int chosungPos, jungsungPos, jongsungPos;
        int nUniCode;

        chosungPos = m_ChoSungTbl.IndexOf(chosung);
        jungsungPos = m_JungSungTbl.IndexOf(jungsung);
        jongsungPos = m_JongSungTbl.IndexOf(jongsung);

        nUniCode = m_UniCodeHangulBase + (chosungPos * 21 + jungsungPos) * 28 + jongsungPos;
        char temp = Convert.ToChar(nUniCode);
        //Debug.Log(temp);

        return temp;
    }


    // 초성 중성 종성을 분리하는 함수
    public char[] JamoSeparate(char c)
    {
        char[] retval = new char[3];

        retval[2] = ' ';
        ushort check = Convert.ToUInt16(c);
        if(check > m_UniCodeHangulLast || check < m_UniCodeHangulBase)
        {
            return null;
        }

        int Code = check - m_UniCodeHangulBase;

        int JongsungCode = Code % 28;           // 종성 코드 분리
        Code = (Code - JongsungCode) / 28;

        int JungsungCode = Code % 21;           // 중성 코드 분리
        Code = (Code - JungsungCode) / 21;

        int ChosungCode = Code;                 // 초성

        retval[0] = m_ChoSungTbl[ChosungCode];
        retval[1] = m_JungSungTbl[JungsungCode];
        retval[2] = m_JongSungTbl[JongsungCode];

        //Debug.LogFormat("{0} {1} {2}", retval[0], retval[1], retval[2]);

        return retval;
    }

    // 조립 할 수 있는지 없는지 체크하는 함수
    public bool CheckJamo(char c, char current)
    {
        char[] retval = JamoSeparate(c);

        // 종성이 없거나 케이스문에 없는 것이라면 조립 가능
        switch(retval[2])
        {
            case 'ㄲ':
            case 'ㄳ':
            case 'ㄵ':
            case 'ㄶ':
            case 'ㄺ':
            case 'ㄻ':
            case 'ㄼ':
            case 'ㄽ':
            case 'ㄾ':
            case 'ㅀ':
            case 'ㅄ':
            case 'ㅆ':
            return true;

            default:
            // 종성이 조립 가능한지 체크
            char temp = CheckConsonant(retval[2], current);

            if(retval[2] == ' ')
            {
                return false;
            }

            // 같다는 것은 조립이 불가능
            else if(temp.CompareTo(retval[2]) == 0)
            {
                // true로 끝내고 Complete로 보낸다.
                return true;
            }

            else
            {
                // false로 끝내고 미조립으로 끝낸다.
                return false;
            }
        }
    }

    // 중성이 조립이 가능한지 체크 리턴값이 pre_c면 조립x
    public char CheckVowel(char pre_c, char current_c)
    {
        char assembly = pre_c;

        switch(pre_c)
        {  // "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";
            case 'ㅗ':
            switch(current_c)
            {
                case 'ㅏ':
                assembly = 'ㅘ';
                break;

                case 'ㅐ':
                assembly = 'ㅙ';
                break;

                case 'ㅣ':
                assembly = 'ㅚ';
                break;
            }
            break;

            case 'ㅜ':
            switch(current_c)
            {
                case 'ㅓ':
                assembly = 'ㅝ';
                break;

                case 'ㅔ':
                assembly = 'ㅞ';
                break;

                case 'ㅣ':
                assembly = 'ㅟ';
                break;
            }
            break;

            case 'ㅡ':
            switch(current_c)
            {
                case 'ㅣ':
                assembly = 'ㅢ';
                break;
            }
            break;

        }

        return assembly;
    }

    // 종성이 조립이 가능한지 체크 리턴값이 pre_c면 조립 x
    public char CheckConsonant(char pre_c, char current_c)
    {
        char assembly = pre_c;

        //" ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ";

        switch(pre_c)
        {
            case 'ㄱ':
            switch(current_c)
            {
                //case 'ㄱ':
                //assembly = 'ㄲ';
                //break;

                case 'ㅅ':
                assembly = 'ㄳ';
                break;
            }
            break;

            case 'ㄴ':
            switch(current_c)
            {
                case 'ㅈ':
                assembly = 'ㄵ';
                break;

                case 'ㅎ':
                assembly = 'ㄶ';
                break;
            }
            break;

            case 'ㄹ':
            switch(current_c)
            {
                case 'ㄱ':
                assembly = 'ㄺ';
                break;
                case 'ㅁ':
                assembly = 'ㄻ';
                break;
                case 'ㅂ':
                assembly = 'ㄼ';
                break;
                case 'ㅅ':
                assembly = 'ㄽ';
                break;
                case 'ㅌ':
                assembly = 'ㄾ';
                break;
                case 'ㅍ':
                assembly = 'ㄿ';
                break;
                case 'ㅎ':
                assembly = 'ㅀ';
                break;

            }
            break;

            case 'ㅂ':
            switch(current_c)
            {
                case 'ㅅ':
                assembly = 'ㅄ';
                break;
            }
            break;

            //case 'ㅅ':
            //switch(current_c)
            //{
            //    case 'ㅅ':
            //    assembly = 'ㅆ';
            //    break;
            //}
            //break;
        }


        return assembly;
    }


    // 조립된 종성을 2개의 문자로 리턴
    public char[] SeparateConsonant(char pre_c)
    {
        char[] separate = new char[2];
        //  " ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ"

        separate[1] = ' ';
        separate[0] = ' '; 

        switch(pre_c)
        {
            case 'ㄲ':
            separate[0] = 'ㄱ';
            separate[1] = 'ㄱ';
            break;

            case 'ㄺ':
            separate[0] = 'ㄹ';
            separate[1] = 'ㄱ';
            break;

            case 'ㄵ':
            separate[0] = 'ㄴ';
            separate[1] = 'ㅈ';
            break;

            case 'ㅀ':
            separate[0] = 'ㄹ';
            separate[1] = 'ㅎ';
            break;

            case 'ㄶ':
            separate[0] = 'ㄴ';
            separate[1] = 'ㅎ';
            break;

            case 'ㄻ':
            separate[0] = 'ㄹ';
            separate[1] = 'ㅁ';
            break;

            case 'ㄼ':
            separate[0] = 'ㄹ';
            separate[1] = 'ㅂ';
            break;

            case 'ㄾ':
            separate[0] = 'ㄹ';
            separate[1] = 'ㅌ';
            break;

            case 'ㄿ':
            separate[0] = 'ㄹ';
            separate[1] = 'ㅍ';
            break;

            case 'ㄳ':
            separate[0] = 'ㄱ';
            separate[1] = 'ㅅ';
            break;
            case 'ㄽ':
            separate[0] = 'ㄹ';
            separate[1] = 'ㅅ';
            break;
            case 'ㅄ':
            separate[0] = 'ㅂ';
            separate[1] = 'ㅅ';
            break;
            case 'ㅆ':
            separate[0] = 'ㅅ';
            separate[1] = 'ㅅ';
            break;

        }



        return separate;
    }


    // 조립된 중성을 2개의 문자로 리턴
    public char[] SeparateVowel(char pre_c)
    {
        char[] separate = new char[2];

        //    "ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ";

        separate[1] = ' ';
        separate[0] = ' ';

        switch(pre_c)
        {
            case 'ㅘ':
            separate[0] = 'ㅗ';
            separate[1] = 'ㅏ';
            break;

            case 'ㅙ':
            separate[0] = 'ㅗ';
            separate[1] = 'ㅐ';
            break;

            case 'ㅚ':
            separate[0] = 'ㅗ';
            separate[1] = 'ㅣ';
            break;

            case 'ㅝ':
            separate[0] = 'ㅜ';
            separate[1] = 'ㅓ';
            break;

            case 'ㅞ':
            separate[0] = 'ㅜ';
            separate[1] = 'ㅔ';
            break;

            case 'ㅟ':
            separate[0] = 'ㅜ';
            separate[1] = 'ㅣ';
            break;

            case 'ㅢ':
            separate[0] = 'ㅡ';
            separate[1] = 'ㅣ';
            break;
        }



        return separate;
    }

}