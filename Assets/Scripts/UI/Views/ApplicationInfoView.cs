using UnityEngine;
using System.Collections;
using LightSide;

public class ApplicationInfoView : MonoBehaviour
{
    [SerializeField] private UniText _gameVersionText;
    private enum GameVersion
    {
        Release,
        Beta,
        Alpha
    }

    [SerializeField] private GameVersion _gameVersion;

    private IEnumerator Start()
    {
        yield return null;

        if (_gameVersionText != null)
        {
            if (_gameVersion == GameVersion.Release)
            {
                _gameVersionText.Text = Application.version + " release";
            }
            else if (_gameVersion == GameVersion.Beta)
            {
                _gameVersionText.Text = Application.version + " beta";
            }
            else
            {
                _gameVersionText.Text = Application.version + " alpha";
            }
        }
    }
}