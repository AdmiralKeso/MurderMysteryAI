using UnityEngine;
using UnityEngine.UI;

// Reads/writes local game settings (audio volumes, resolution, fullscreen)
// via PlayerPrefs. Replaces the old settings.blade.php form, which posted to
// a route the Laravel app never actually implemented — this version works.
public class SettingsController : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider effectsVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private CycleSelector screenTypeSelector;
    [SerializeField] private CycleSelector resolutionSelector;
    [SerializeField] private Button saveButton;

    private static readonly (int width, int height)[] Resolutions =
    {
        (1920, 1080),
        (1600, 900),
        (1280, 720),
    };

    void Awake()
    {
        saveButton.onClick.AddListener(Save);
        screenTypeSelector.Setup(new[] { "Fullscreen", "Windowed" });
        resolutionSelector.Setup(new[] { "1920 x 1080", "1600 x 900", "1280 x 720" });
    }

    void OnEnable()
    {
        Load();
    }

    private void Load()
    {
        masterVolumeSlider.value = PlayerPrefs.GetFloat("settings.master_volume", 80f);
        effectsVolumeSlider.value = PlayerPrefs.GetFloat("settings.effects_volume", 80f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("settings.music_volume", 60f);
        screenTypeSelector.SetIndex(PlayerPrefs.GetInt("settings.fullscreen", 1) == 1 ? 0 : 1);

        int savedResIndex = PlayerPrefs.GetInt("settings.resolution_index", 0);
        resolutionSelector.SetIndex(Mathf.Clamp(savedResIndex, 0, Resolutions.Length - 1));
    }

    private void Save()
    {
        PlayerPrefs.SetFloat("settings.master_volume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("settings.effects_volume", effectsVolumeSlider.value);
        PlayerPrefs.SetFloat("settings.music_volume", musicVolumeSlider.value);
        PlayerPrefs.SetInt("settings.fullscreen", screenTypeSelector.CurrentIndex == 0 ? 1 : 0);
        PlayerPrefs.SetInt("settings.resolution_index", resolutionSelector.CurrentIndex);
        PlayerPrefs.Save();

        AudioListener.volume = masterVolumeSlider.value / 100f;

        var (width, height) = Resolutions[resolutionSelector.CurrentIndex];
        bool fullscreen = screenTypeSelector.CurrentIndex == 0;
        Screen.SetResolution(width, height, fullscreen);
    }
}
