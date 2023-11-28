using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using KModkit;
using NumberCruncher;
using Rnd = UnityEngine.Random;

public class NumberCruncherScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public TextMesh[] ScreenTexts;
    public Text ScoreCard;
    public Text ScoreCardDouble;
    public Sprite[] CalcSprites;
    public Image CalcDisplay;
    public Image CalcDisplayDouble;
    public SpriteRenderer Speaker;
    public GameObject[] Cogs;

    private KeyCode[] TypableKeysPrimary =
    {
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Backspace, KeyCode.Return
    };
    private KeyCode[] TypableKeysSecondary =
    {
        KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9, KeyCode.Backspace, KeyCode.KeypadEnter
    };
    private static CalculationFactory CalcFactory;
    private Calculation CurrCalc;
    private Coroutine[] ButtonAnimCoroutines;
    private Coroutine HelpCoroutine;
    private KMAudio.KMAudioRef[] Sounds = new KMAudio.KMAudioRef[2];
    private int Score, FlickerStrength;
    private int SuccessThreshold = 20;
    private float[] CogSpeeds = new[] { .05f, .075f, .125f, .025f, .08f, .15f, .035f, .08f, .15f, .035f, .065f, .125f };  //in revs/second
    private const float CogVarianceLower = 0.5f;
    private const float CogVarianceUpper = 1.25f;
    private float CogBoost = 1f;
    private string InputText = "";
    private bool DisplayingMistakes, Focused, WaitingForScore, Helping, Solved, WillSolve;

    private Settings _Settings;

    private Sprite FindSprite(string name)
    {
        var sprites = CalcSprites.Where(x => x.name == name);
        if (sprites.Count() > 0)
            return sprites.First();
        throw new Exception($"Sprite {name} not found!");
    }

    class Settings
    {
        public int PointThreshold = 20;
    }

    void GetSettings()
    {
        var SettingsConfig = new ModConfig<Settings>("NumberCruncher");
        _Settings = SettingsConfig.Settings; // This reads the settings from the file, or creates a new file if it does not exist
        SettingsConfig.Settings = _Settings; // This writes any updates or fixes if there's an issue with the file
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        GetSettings();
        SuccessThreshold = _Settings.PointThreshold;
        ButtonAnimCoroutines = new Coroutine[Buttons.Length];
        for (int i = 0; i < Buttons.Length; i++)
        {
            int x = i;
            Buttons[x].OnInteract += delegate { ButtonPress(x); return false; };
        }
        ScoreCard.text = "";
        ScoreCardDouble.text = "";
        Module.OnActivate += delegate
        {
            Calculate(true);
            StartCoroutine(ChangeScore(0, 1, true));
            StartCoroutine(ChangeCalc());
            AssignFlickerStrength();
            for (int i = 0; i < ScreenTexts.Length; i++)
                StartCoroutine(TextFlicker(ScreenTexts[i], ScreenTexts[i].color));
        };
        Module.GetComponent<KMSelectable>().OnFocus += delegate { Focused = true; };
        Module.GetComponent<KMSelectable>().OnDefocus += delegate { Focused = false; };
        ScreenTexts[0].text = ScreenTexts[1].text = ScreenTexts[2].text = "";
        for (int i = 0; i < Cogs.Length; i++)
        {
            CogSpeeds[i] *= Rnd.Range(CogVarianceLower, CogVarianceUpper) * new[] { 1, -1 }.PickRandom();
            StartCoroutine(AnimCog(i));
        }
        Bomb.OnBombSolved += delegate { if (Helping) StopHelping(); };
        Bomb.OnBombExploded += delegate { if (Helping) StopHelping(); };
    }

    // Use this for initialization
    void Start()
    {
        if (CalcFactory == null)
            CalcFactory = new CalculationFactory();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < TypableKeysPrimary.Count(); i++)
            if ((Input.GetKeyDown(TypableKeysPrimary[i]) || Input.GetKeyDown(TypableKeysSecondary[i])) && Focused)
                Buttons[i].OnInteract();
    }

    void Calculate(bool first = false)
    {
        CurrCalc = CalcFactory.Calculation(Score, SuccessThreshold, CurrCalc);
        ScreenTexts[0].text = CurrCalc.GetInputs()[0];
        ScreenTexts[1].text = CurrCalc.GetInputs()[1];
        Debug.LogFormat("[Number Cruncher #{0}] The {1} calculation is {2}, worth {3}.", _moduleID, first ? "first" : "next", CurrCalc.Name, CurrCalc.Score + (CurrCalc.Score == 1 ? " point" : " points"));
        if (ScreenTexts[1].text == "")
            Debug.LogFormat("[Number Cruncher #{0}] This calculation uses one number, which is {1}.", _moduleID, ScreenTexts[0].text);
        else
            Debug.LogFormat("[Number Cruncher #{0}] This calculation uses two numbers, which are {1} and {2}.", _moduleID, ScreenTexts[0].text, ScreenTexts[1].text);
        Debug.LogFormat("[Number Cruncher #{0}] The answer is {1}.", _moduleID, CurrCalc.GetAnswer());
    }

    void ButtonPress(int pos)
    {
        if (WillSolve || DisplayingMistakes || Helping || WaitingForScore)
            Audio.PlaySoundAtTransform("press", Buttons[pos].transform);
        else
            Audio.PlaySoundAtTransform("press" + (pos == 11 && !WaitingForScore ? (CurrCalc.GetAnswer() != InputText ? " strike" : " alt") : ""), Buttons[pos].transform);
        if (ButtonAnimCoroutines[pos] != null)
            StopCoroutine(ButtonAnimCoroutines[pos]);
        ButtonAnimCoroutines[pos] = StartCoroutine(ButtonAnim(pos));
        Buttons[pos].AddInteractionPunch();
        if (!WillSolve && !DisplayingMistakes)
        {
            if (pos == 10)
            {
                if (InputText != "")
                {
                    InputText = InputText.Substring(0, InputText.Length - 1);
                    ScreenTexts[2].text = InputText;
                    AssignFlickerStrength();
                }
            }
            else if (pos == 11)
            {
                if (!WaitingForScore && !Helping)
                {
                    if (CurrCalc.GetAnswer() == InputText)
                    {
                        var prev = Score;
                        Score += CurrCalc.Score;
                        if (Score >= SuccessThreshold)
                        {
                            WillSolve = true;
                            Debug.LogFormat("[Number Cruncher #{0}] You submitted {1}, which was correct. Module solved!", _moduleID, InputText);
                        }
                        else
                            Debug.LogFormat("[Number Cruncher #{0}] You submitted {1}, which was correct. You now have {2}.", _moduleID, InputText, Score + (Score == 1 ? " point" : " points"));
                        Reset();
                        StartCoroutine(ChangeScore(prev, Score));
                        if (Score >= SuccessThreshold)
                            StartCoroutine(ChangeCalc(FindSprite("Solved")));
                        else
                        {
                            Calculate();
                            StartCoroutine(ChangeCalc());
                            StartCoroutine(BoostCogs(5f, 0.5f));
                            Helping = false;
                            if (Helping)
                            {
                                if (Sounds[0] != null)
                                    Sounds[0].StopSound();
                                if (Sounds[1] != null)
                                    Sounds[1].StopSound();
                                Audio.PlaySoundAtTransform("stop", Buttons[12].transform);
                                Speaker.transform.localScale = new Vector3(0.375f, 0.375f, Speaker.transform.localScale.z);
                            }
                        }
                        AssignFlickerStrength();
                    }
                    else
                    {
                        Module.HandleStrike();
                        var temp = new List<string>();
                        for (int i = 0; i < 12; i++)
                            if (InputText.Length < i + 1 || CurrCalc.GetAnswer()[i] != InputText[i])
                                temp.Add((i + 1).ToString());
                        if (temp.Count > 1)
                        {
                            temp[temp.Count - 2] = temp[temp.Count - 2] + " and " + temp.Last();
                            temp.RemoveAt(temp.Count - 1);
                        }
                        Debug.LogFormat("[Number Cruncher #{0}] You submitted {1}, which was incorrect{2} Strike!", _moduleID, InputText == "" ? "nothing" : InputText,
                            InputText == "" ? "." : (temp.Count() == 1 ? ": Digit " : ": Digits ") + temp.Join(", ") + (temp.Count() == 1 ? " was wrong." : " were wrong."));
                        StartCoroutine(DisplayIncorrectDigits());
                    }
                }
            }
            else if (pos == 12)
            {
                if (Helping)
                    StopHelping();
                else
                    HelpCoroutine = StartCoroutine(PlayHelpMessage(CurrCalc.HasHelpMessage ? CurrCalc.Name : "unknown", CurrCalc.HasHelpMessage ? CurrCalc.SoundLength : 2f + (15224f / 44100f)));
            }
            else if (InputText.Length < 12)
            {
                InputText += pos.ToString();
                ScreenTexts[2].text = InputText;
                AssignFlickerStrength();
            }
        }
        if (Solved && pos == 8)
            Audio.PlaySoundAtTransform("8", Buttons[8].transform);
        //else if (Solved && pos == 12)
        //{
        //    if (Helping)
        //    {
        //        if (Sounds[0] != null)
        //            Sounds[0].StopSound();
        //        if (Sounds[1] != null)
        //            Sounds[1].StopSound();
        //        if (HelpCoroutine != null)
        //            StopCoroutine(HelpCoroutine);
        //        Audio.PlaySoundAtTransform("stop", Buttons[12].transform);
        //        Helping = false;
        //        Speaker.transform.localScale = new Vector3(0.375f, 0.375f, Speaker.transform.localScale.z);
        //    }
        //    else
        //        HelpCoroutine = StartCoroutine(PlayHelpMessage("Solved", 4f + (36668f / 44100f)));
        //}
    }

    void StopHelping()
    {
        if (Sounds[0] != null)
            Sounds[0].StopSound();
        if (Sounds[1] != null)
            Sounds[1].StopSound();
        if (HelpCoroutine != null)
            StopCoroutine(HelpCoroutine);
        Audio.PlaySoundAtTransform("stop", Buttons[12].transform);
        Helping = false;
        Speaker.transform.localScale = new Vector3(0.375f, 0.375f, Speaker.transform.localScale.z);
    }

    void Reset()
    {
        InputText = ScreenTexts[2].text = "";
        ScreenTexts[0].text = ScreenTexts[1].text = "";
        AssignFlickerStrength();
    }

    void AssignFlickerStrength()
    {
        FlickerStrength = 0;
        for (int i = 0; i < ScreenTexts.Length; i++)
            FlickerStrength += ScreenTexts[i].text.Length;
    }

    void Solve()
    {
        Module.HandlePass();
        Solved = true;
        Audio.PlaySoundAtTransform("solve", transform);
        if (SuccessThreshold == 20)
            Audio.PlaySoundAtTransform("cogs spin", transform);
        StartCoroutine(ChangeScoreSolved("$$"));
        ScreenTexts[0].text = ScreenTexts[1].text = ScreenTexts[2].text = "";
        StartCoroutine(BoostCogs(25f, 4f, true));
    }

    private IEnumerator DisplayIncorrectDigits()
    {
        DisplayingMistakes = true;
        var temp = "";
        var chars = "1234567890Ab";
        for (int i = 0; i < 12; i++)
            temp += (InputText.Length < i + 1 || CurrCalc.GetAnswer()[i] != InputText[i]) ? chars[i] : ' ';
        ScreenTexts[2].text = temp;
        Audio.PlaySoundAtTransform("blink on", transform);
        float timer = 0;
        while (timer < 0.75f)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        ScreenTexts[2].text = "";
        //Audio.PlaySoundAtTransform("blink off", transform);
        timer = 0;
        while (timer < 0.125f)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        for (int i = 0; i < 3; i++)
        {
            ScreenTexts[2].text = temp;
            Audio.PlaySoundAtTransform("blink on", transform);
            timer = 0;
            while (timer < 0.125f)
            {
                yield return null;
                timer += Time.deltaTime;
            }
            ScreenTexts[2].text = "";
            //Audio.PlaySoundAtTransform("blink off", transform);
            timer = 0;
            while (timer < 0.125f)
            {
                yield return null;
                timer += Time.deltaTime;
            }
        }
        ScreenTexts[2].text = InputText;
        DisplayingMistakes = false;
    }

    private IEnumerator AnimCog(int pos)
    {
        if (SuccessThreshold == 20)
            while (true)
            {
                Cogs[pos].transform.localEulerAngles += new Vector3(0, CogSpeeds[pos] * CogBoost * Time.deltaTime * 360, 0);
                yield return null;
            }
        yield return null;
    }

    private IEnumerator BoostCogs(float boost, float duration, bool solving = false)
    {
        CogBoost = boost;
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            if (solving)
                CogBoost = Easing.OutExpo(timer, boost, 0, duration);
        }
        CogBoost = !solving ? 1f : 0;
    }

    private IEnumerator ButtonAnim(int pos, float duration = 0.075f)
    {
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, -0.01f * (timer / duration), Buttons[pos].transform.localPosition.z);
        }
        duration = 0.15f;
        timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, -0.01f * (1 - (timer / duration)), Buttons[pos].transform.localPosition.z);
        }
        Buttons[pos].transform.localPosition = new Vector3(Buttons[pos].transform.localPosition.x, 0, Buttons[pos].transform.localPosition.z);
    }

    private IEnumerator PlayHelpMessage(string name, float length)
    {
        Helping = true;
        Audio.PlaySoundAtTransform("play", Buttons[12].transform);
        Sounds[0] = Audio.PlaySoundAtTransformWithRef("whir", Speaker.transform);
        float timer = 0;
        while (timer < 0.5f)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        Sounds[1] = Audio.HandlePlaySoundAtTransformWithRef(name, Speaker.transform, false);
        timer = 0;
        while (timer < length)
        {
            yield return null;
            timer += Time.deltaTime;
            Speaker.transform.localScale = new Vector3(0.76f - Speaker.transform.localScale.x, 0.76f - Speaker.transform.localScale.y, Speaker.transform.localScale.z);
        }
        Helping = false;
        Sounds[0].StopSound();
        if (Sounds[1] != null)
            Sounds[1].StopSound();
        Audio.PlaySoundAtTransform("stop", Buttons[12].transform);
        Speaker.transform.localScale = new Vector3(0.375f, 0.375f, Speaker.transform.localScale.z);
    }

    private IEnumerator ChangeScore(int prev, int now, bool init = false, float duration = 0.06f)
    {
        WaitingForScore = true;
        for (int i = 0; i < now - prev; i++)
        {
            if (prev + i == SuccessThreshold - 1)
            {
                Solve();
                break;
            }
            Audio.PlaySoundAtTransform("flip", ScoreCard.transform);
            if (init)
                ScoreCardDouble.text = "00";
            else
                ScoreCardDouble.text = (prev + i + 1).ToString("00");
            float timer = 0;
            while (timer < duration)
            {
                yield return null;
                timer += Time.deltaTime;
                ScoreCard.transform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.down, timer / duration);
                ScoreCardDouble.transform.localPosition = Vector3.Lerp(Vector3.up, Vector3.zero, timer / duration);
            }
            ScoreCard.text = ScoreCardDouble.text;
            ScoreCard.transform.localPosition = Vector3.zero;
            ScoreCardDouble.transform.localPosition = Vector3.up;
        }
        WaitingForScore = false;
    }

    private IEnumerator ChangeScoreSolved(string text, float duration = 0.06f)
    {
        yield return "solve";
        ScoreCardDouble.text = text;
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            ScoreCard.transform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.down, timer / duration);
            ScoreCardDouble.transform.localPosition = Vector3.Lerp(Vector3.up, Vector3.zero, timer / duration);
        }
        ScoreCard.text = ScoreCardDouble.text;
        ScoreCard.transform.localPosition = Vector3.zero;
        ScoreCardDouble.transform.localPosition = Vector3.up;
    }

    private IEnumerator ChangeCalc(Sprite custom = null, float duration = 0.06f)
    {
        Audio.PlaySoundAtTransform("flip", ScoreCard.transform);
        if (custom == null)
            CalcDisplayDouble.sprite = FindSprite(CurrCalc.Name);
        else
            CalcDisplayDouble.sprite = custom;
        float timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            CalcDisplay.transform.localPosition = Vector3.Lerp(Vector3.zero, Vector3.down, timer / duration);
            CalcDisplayDouble.transform.localPosition = Vector3.Lerp(Vector3.up, Vector3.zero, timer / duration);
        }
        CalcDisplay.sprite = CalcDisplayDouble.sprite;
        CalcDisplay.transform.localPosition = Vector3.zero;
        CalcDisplayDouble.transform.localPosition = Vector3.up;
    }

    private IEnumerator TextFlicker(TextMesh target, Color colour, float lower = 0.6f, float upper = 0.7f)
    {
        while (true)
        {
            target.color = new Color(target.color.r, target.color.g, target.color.b, Rnd.Range(lower, upper) - (FlickerStrength * (1 / 24f) * 0.2f));
            yield return null;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} 0123456789*#' to press buttons 0-9, then press the submit button, then press the reset button. Use '!{0} play' to press the button labelled \"HELP\".";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (command == "play")
        {
            yield return null;
            Buttons.Last().OnInteract();
        }
        else
        {
            foreach (char character in command)
                if (!"0123456789#*".Contains(character))
                {
                    yield return "sendtochaterror Invalid command.";
                    yield break;
                }
            yield return null;
            foreach (char character in command)
            {
                Buttons["0123456789#*".IndexOf(character)].OnInteract();
                float reference = Time.time;
                yield return new WaitUntil(() => Time.time - reference > 0.075f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!WillSolve)
        {
            while (InputText != CurrCalc.GetAnswer().Substring(0, InputText.Length))
            {
                Buttons[10].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            yield return null;
            foreach (char character in CurrCalc.GetAnswer().Substring(InputText.Length, 12 - InputText.Length))
            {
                Buttons["0123456789".IndexOf(character)].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            Buttons[11].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
