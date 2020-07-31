using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of 101 Dalmatians
/// Created by Timwi
/// </summary>
public class OneHundredAndOneDalmatiansModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public Texture[] Furspots;
    public GameObject[] Dalmatians;
    public GameObject DalmatianParent;
    public MeshRenderer Fur;
    public KMSelectable LeftArrow, RightArrow, Submit;
    public TextMesh[] NameDisplays;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private int _solution;
    private int _curDalmatianIndex;
    private Coroutine _longPress;
    private bool _solved;

    private static readonly string[] _dalmatians = new[] { "Blackear", "Blackie", "Blob", "Blot", "Bon-Bon", "Bravo", "Brownie", "Bulgey", "Bump", "Cadpig", "Corky", "D.J.", "Da Vinci", "Dante", "Dash", "Dawkins", "Deja Vu", "Dingo", "Dipper", "Dipstick", "Disco", "Disel", "Dolly", "Dorothy", "Dot", "Duke", "Dylan", "Fatty", "Fidget", "Flapper", "Football", "Freckles", "Furrball", "Guy", "Growly", "Ham", "Harvey", "Holly", "Hoofer", "Hoover", "Hungry", "Inky", "Jewel", "Jolly", "Kirby", "Latch", "Lenny", "Leno", "Lipdip", "Lucky", "Ludo", "Lugnut", "Lumpy", "Missy", "Nosey", "Pandy", "Patches", "Penny", "Pepper", "Perdita", "Pickle", "Plato", "Playdoh", "Pointy", "Pokey", "Polly", "Pongo", "Pooh", "Puddles", "Purdy", "Queeny", "Roger", "Roly Poly", "Rover", "Sa-Sa", "Salter", "Scooter", "Scottie", "Sleepy", "Smokey", "Sniff", "Spanky", "Spark", "Spatter", "Speedy", "Sport", "Spot", "Spotty", "Steve", "Sugar", "Swifty", "Thunder", "Tiger", "Tiresome", "Tripod", "Two-Tone", "Wags", "Whitey", "Whizzer", "Yank", "Yoyo" };

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _solved = false;

        // Rule seed
        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[101 Dalmatians #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);

        var skip = rnd.Next(0, 100);
        for (var i = 0; i < skip; i++)
            rnd.NextDouble();
        rnd.ShuffleFisherYates(Furspots);

        // Decide which fur pattern to show on the module and in what rotation.
        _solution = Rnd.Range(0, 101);
        Fur.material.mainTexture = Furspots[_solution];
        var moduleRotation = Rnd.Range(0, 360f);
        Fur.transform.localEulerAngles = new Vector3(0, moduleRotation, 0);
        _curDalmatianIndex = Rnd.Range(0, 101);
        UpdateName();

        Debug.LogFormat(@"<101 Dalmatians #{0}> Name initially shown: {1}", _moduleId, _dalmatians[_curDalmatianIndex]);
        Debug.LogFormat(@"[101 Dalmatians #{0}] Solution: {1}", _moduleId, _dalmatians[_solution]);
        Debug.LogFormat(@"[101 Dalmatians #{0}] Showing fur pattern {1} rotated {2}° clockwise", _moduleId, Furspots[_solution].name.Substring(3), Math.Round(moduleRotation));

        LeftArrow.OnInteract = delegate { _curDalmatianIndex = (_curDalmatianIndex + 100) % 101; UpdateName(); startLongPress(100); return false; };
        RightArrow.OnInteract = delegate { _curDalmatianIndex = (_curDalmatianIndex + 1) % 101; UpdateName(); startLongPress(1); return false; };

        LeftArrow.OnInteractEnded = RightArrow.OnInteractEnded = delegate
        {
            if (_longPress != null)
                StopCoroutine(_longPress);
        };

        Submit.OnInteract = delegate
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            Submit.AddInteractionPunch();

            if (_solved)
                return false;
            if (_curDalmatianIndex == _solution)
            {
                Debug.LogFormat(@"[101 Dalmatians #{0}] You submitted {1}. Correct.", _moduleId, _dalmatians[_curDalmatianIndex]);
                Module.HandlePass();
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                LeftArrow.gameObject.SetActive(false);
                RightArrow.gameObject.SetActive(false);
                StartCoroutine(solveAnimationPart1());
                StartCoroutine(solveAnimationPart2());
                _solved = true;
            }
            else
            {
                Debug.LogFormat(@"[101 Dalmatians #{0}] You submitted {1}. Strike.", _moduleId, _dalmatians[_curDalmatianIndex]);
                Module.HandleStrike();
            }
            return false;
        };
    }

    private IEnumerator solveAnimationPart1()
    {
        yield return new WaitForSeconds(.3f);
        var duration = .6f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            Submit.transform.localPosition = new Vector3(Easing.InOutQuad(elapsed, 0, -0.026f, duration), 0.01501f, .03f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        Submit.transform.localPosition = new Vector3(-0.026f, 0.01501f, .03f);
    }

    private IEnumerator solveAnimationPart2()
    {
        yield return new WaitForSeconds(.75f);
        var ix = Rnd.Range(0, Dalmatians.Length);
        Dalmatians[ix].SetActive(true);
        var scale = DalmatianParent.transform.localScale;
        var duration = .3f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            var x = elapsed / duration / 1.5f;
            DalmatianParent.transform.localScale = new Vector3(scale.x, scale.y, 4.5f * x * (1 - x));
            yield return null;
            elapsed += Time.deltaTime;
        }
        DalmatianParent.transform.localScale = scale;
    }

    private void startLongPress(int dir)
    {
        if (_longPress != null)
            StopCoroutine(_longPress);

        _longPress = StartCoroutine(longPress(dir));
    }

    private IEnumerator longPress(int dir)
    {
        yield return new WaitForSeconds(.37f);
        while (true)
        {
            _curDalmatianIndex = (_curDalmatianIndex + dir) % 101;
            UpdateName();
            yield return new WaitForSeconds(.02f);
        }
    }

    private void UpdateName()
    {
        foreach (var disp in NameDisplays)
            disp.text = _dalmatians[_curDalmatianIndex];
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} Perdita [submit the name Perdita]";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        if (_solved)
            yield break;

        command = command.Trim();
        if (!_dalmatians.Contains(command, StringComparer.InvariantCultureIgnoreCase))
            yield break;

        yield return null;
        if (!_dalmatians[_curDalmatianIndex].Equals(command, StringComparison.InvariantCultureIgnoreCase))
        {
            RightArrow.OnInteract();
            while (!_dalmatians[_curDalmatianIndex].Equals(command, StringComparison.InvariantCultureIgnoreCase))
                yield return "trycancel";
            RightArrow.OnInteractEnded();
            yield return new WaitForSeconds(.6f);
        }
        yield return new[] { Submit };
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        RightArrow.OnInteract();
        while (_curDalmatianIndex != _solution)
            yield return null;
        RightArrow.OnInteractEnded();
        yield return new WaitForSeconds(.1f);
        Submit.OnInteract();
        yield return new WaitForSeconds(.1f);
    }
}
