﻿using System.ComponentModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STranslate.Helper;
using STranslate.Log;
using STranslate.Model;
using STranslate.Util;

namespace STranslate.ViewModels.Preference.Translator;

public partial class TranslatorDeepLX : TranslatorBase, ITranslator
{
    #region Constructor

    public TranslatorDeepLX() : this(Guid.NewGuid(), "http://127.0.0.1:1188/translate", "DeepLX")
    {
    }

    public TranslatorDeepLX(
        Guid guid,
        string url,
        string name = "",
        IconType icon = IconType.DeepL,
        string appId = "",
        string appKey = "",
        bool isEnabled = true,
        ServiceType type = ServiceType.DeepLXService
    )
    {
        Identify = guid;
        Url = url;
        Name = name;
        Icon = icon;
        AppID = appId;
        AppKey = appKey;
        IsEnabled = isEnabled;
        Type = type;
    }

    #endregion Constructor

    #region Properties

    [ObservableProperty] private Guid _identify = Guid.Empty;

    [JsonIgnore] [ObservableProperty] private ServiceType _type = 0;

    [JsonIgnore] [ObservableProperty] private bool _isEnabled = true;

    [JsonIgnore] [ObservableProperty] private string _name = string.Empty;

    [JsonIgnore] [ObservableProperty] private IconType _icon = IconType.DeepL;

    [JsonIgnore]
    [ObservableProperty]
    [property: DefaultValue("")]
    [property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    private string _url = string.Empty;

    [JsonIgnore]
    [ObservableProperty]
    [property: DefaultValue("")]
    [property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    private string _appID = string.Empty;

    [JsonIgnore]
    [ObservableProperty]
    [property: DefaultValue("")]
    [property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    private string _appKey = string.Empty;

    [JsonIgnore] public BindingList<UserDefinePrompt> UserDefinePrompts { get; set; } = [];

    [JsonIgnore] [ObservableProperty] private bool _autoExecute = true;

    [JsonIgnore] [ObservableProperty] [property: JsonIgnore]
    private TranslationResult _data = TranslationResult.Reset;

    [JsonIgnore] [ObservableProperty] [property: JsonIgnore]
    private bool _isExecuting;

    [JsonIgnore]
    [ObservableProperty]
    [property: DefaultValue("")]
    [property: JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    private string _token = string.Empty;

    #endregion Properties

    #region Translator Test

    [property: JsonIgnore] [ObservableProperty]
    private bool _isTesting;

    [property: JsonIgnore]
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task TestAsync(CancellationToken token)
    {
        var result = "";
        var isCancel = false;
        try
        {
            IsTesting = true;
            var reqModel = new RequestModel("你好", LangEnum.zh_cn, LangEnum.en);
            var ret = await TranslateAsync(reqModel, token);

            result = ret.IsSuccess ? "验证成功" : "验证失败";
        }
        catch (OperationCanceledException)
        {
            isCancel = true;
        }
        catch (Exception)
        {
            result = "验证失败";
        }
        finally
        {
            IsTesting = false;
            if (!isCancel) ToastHelper.Show(result, WindowType.Preference);
        }
    }

    #endregion Translator Test

    #region Interface Implementation

    public async Task<TranslationResult> TranslateAsync(object request, CancellationToken canceltoken)
    {
        if (request is not RequestModel req)
            throw new Exception($"请求数据出错: {request}");

        // 检查是否支持的语言
        var convSource = LangConverter(req.SourceLang) ?? throw new Exception($"该服务不支持{req.SourceLang.GetDescription()}");
        var convTarget = TargetLangConverter(req.TargetLang) ?? throw new Exception($"该服务不支持{req.TargetLang.GetDescription()}");

        var reqStr = JsonConvert.SerializeObject(new
        {
            text = req.Text,
            source_lang = convSource,
            target_lang = convTarget
        });

        var authToken = string.IsNullOrEmpty(Token) ? [] : new Dictionary<string, string> { { "Authorization", $"Bearer {Token}" } };

        try
        {
            var resp = await HttpUtil.PostAsync(Url, reqStr, null, authToken, canceltoken).ConfigureAwait(false) ??
                       throw new Exception("请求结果为空");
            var data = JsonConvert.DeserializeObject<JObject>(resp)?["data"]?.ToString() ?? throw new Exception(resp);
            return TranslationResult.Success(data);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            var msg = $"请检查服务是否可以正常访问: {Name} ({Url}).";
            throw new HttpRequestException(msg);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (ex.InnerException is Exception innEx)
            {
                var innMsg = JsonConvert.DeserializeObject<JObject>(innEx.Message);
                msg += $" {innMsg?["message"]}";
                LogService.Logger.Error($"({Name})({Identify}) raw content:\n{innEx.Message}");
            }

            msg = msg.Trim();

            throw new Exception(msg);
        }
    }

    public Task TranslateAsync(object request, Action<string> onDataReceived, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    public ITranslator Clone()
    {
        return new TranslatorDeepLX
        {
            Identify = Identify,
            Type = Type,
            IsEnabled = IsEnabled,
            Icon = Icon,
            Name = Name,
            Url = Url,
            Data = TranslationResult.Reset,
            AppID = AppID,
            AppKey = AppKey,
            AutoExecute = AutoExecute,
            Token = Token,
            IsExecuting = IsExecuting,
            IsTranslateBackExecuting = IsTranslateBackExecuting,
            AutoExecuteTranslateBack = AutoExecuteTranslateBack,
        };
    }

    /// <summary>
    ///     https://developers.deepl.com/docs/zh/resources/supported-languages#source-languages
    /// </summary>
    /// <param name="lang"></param>
    /// <returns></returns>
    public string? LangConverter(LangEnum lang)
    {
        return lang switch
        {
            LangEnum.auto => "auto",
            LangEnum.zh_cn => "ZH",
            LangEnum.zh_tw => "ZH",
            LangEnum.yue => "ZH",
            LangEnum.en => "EN",
            LangEnum.ja => "JA",
            LangEnum.ko => "KO",
            LangEnum.fr => "FR",
            LangEnum.es => "ES",
            LangEnum.ru => "RU",
            LangEnum.de => "DE",
            LangEnum.it => "IT",
            LangEnum.tr => "TR",
            LangEnum.pt_pt => "PT-PT",
            LangEnum.pt_br => "PT-BR",
            LangEnum.vi => null,
            LangEnum.id => "ID",
            LangEnum.th => null,
            LangEnum.ms => null,
            LangEnum.ar => "AR",
            LangEnum.hi => null,
            LangEnum.mn_cy => null,
            LangEnum.mn_mo => null,
            LangEnum.km => null,
            LangEnum.nb_no => "NB",
            LangEnum.nn_no => "NB",
            LangEnum.fa => null,
            LangEnum.sv => "SV",
            LangEnum.pl => "PL",
            LangEnum.nl => "NL",
            LangEnum.uk => null,
            _ => "auto"
        };
    }

    public string? TargetLangConverter(LangEnum lang)
    {
        return lang switch
        {
            LangEnum.auto => "auto",
            LangEnum.zh_cn => "ZH-HANS",
            LangEnum.zh_tw => "ZH-HANT",
            LangEnum.yue => "ZH",
            LangEnum.en => "EN",
            LangEnum.ja => "JA",
            LangEnum.ko => "KO",
            LangEnum.fr => "FR",
            LangEnum.es => "ES",
            LangEnum.ru => "RU",
            LangEnum.de => "DE",
            LangEnum.it => "IT",
            LangEnum.tr => "TR",
            LangEnum.pt_pt => "PT-PT",
            LangEnum.pt_br => "PT-BR",
            LangEnum.vi => null,
            LangEnum.id => "ID",
            LangEnum.th => null,
            LangEnum.ms => null,
            LangEnum.ar => "AR",
            LangEnum.hi => null,
            LangEnum.mn_cy => null,
            LangEnum.mn_mo => null,
            LangEnum.km => null,
            LangEnum.nb_no => "NB",
            LangEnum.nn_no => "NB",
            LangEnum.fa => null,
            LangEnum.sv => "SV",
            LangEnum.pl => "PL",
            LangEnum.nl => "NL",
            LangEnum.uk => null,
            _ => "auto"
        };
    }

    #endregion Interface Implementation
}