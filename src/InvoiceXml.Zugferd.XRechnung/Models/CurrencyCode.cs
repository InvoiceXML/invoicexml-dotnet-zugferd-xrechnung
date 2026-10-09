namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// ISO 4217 currency code. Sent and received as the three-letter code, for example <c>"EUR"</c>.
/// </summary>
public enum CurrencyCode
{
    /// <summary>Euro</summary>
    EUR,

    /// <summary>UAE dirham</summary>
    AED,

    /// <summary>Afghan afghani</summary>
    AFN,

    /// <summary>Albanian lek</summary>
    ALL,

    /// <summary>Armenian dram</summary>
    AMD,

    /// <summary>Netherlands Antillean guilder</summary>
    ANG,

    /// <summary>Angolan kwanza</summary>
    AOA,

    /// <summary>Argentine peso</summary>
    ARS,

    /// <summary>Australian dollar</summary>
    AUD,

    /// <summary>Aruban florin</summary>
    AWG,

    /// <summary>Azerbaijani manat</summary>
    AZN,

    /// <summary>Bosnia and Herzegovina convertible mark</summary>
    BAM,

    /// <summary>Barbados dollar</summary>
    BBD,

    /// <summary>Bangladeshi taka</summary>
    BDT,

    /// <summary>Bulgarian lev</summary>
    BGN,

    /// <summary>Bahraini dinar</summary>
    BHD,

    /// <summary>Burundian franc</summary>
    BIF,

    /// <summary>Bermudian dollar</summary>
    BMD,

    /// <summary>Brunei dollar</summary>
    BND,

    /// <summary>Boliviano</summary>
    BOB,

    /// <summary>Brazilian real</summary>
    BRL,

    /// <summary>Bahamian dollar</summary>
    BSD,

    /// <summary>Bhutanese ngultrum</summary>
    BTN,

    /// <summary>Botswana pula</summary>
    BWP,

    /// <summary>Belarusian ruble</summary>
    BYN,

    /// <summary>Belize dollar</summary>
    BZD,

    /// <summary>Canadian dollar</summary>
    CAD,

    /// <summary>Congolese franc</summary>
    CDF,

    /// <summary>Swiss franc</summary>
    CHF,

    /// <summary>Chilean peso</summary>
    CLP,

    /// <summary>Renminbi (Chinese yuan)</summary>
    CNY,

    /// <summary>Colombian peso</summary>
    COP,

    /// <summary>Costa Rican colon</summary>
    CRC,

    /// <summary>Cuban peso</summary>
    CUP,

    /// <summary>Cape Verdean escudo</summary>
    CVE,

    /// <summary>Czech koruna</summary>
    CZK,

    /// <summary>Djiboutian franc</summary>
    DJF,

    /// <summary>Danish krone</summary>
    DKK,

    /// <summary>Dominican peso</summary>
    DOP,

    /// <summary>Algerian dinar</summary>
    DZD,

    /// <summary>Egyptian pound</summary>
    EGP,

    /// <summary>Eritrean nakfa</summary>
    ERN,

    /// <summary>Ethiopian birr</summary>
    ETB,

    /// <summary>Fiji dollar</summary>
    FJD,

    /// <summary>Falkland Islands pound</summary>
    FKP,

    /// <summary>Pound sterling</summary>
    GBP,

    /// <summary>Georgian lari</summary>
    GEL,

    /// <summary>Ghanaian cedi</summary>
    GHS,

    /// <summary>Gibraltar pound</summary>
    GIP,

    /// <summary>Gambian dalasi</summary>
    GMD,

    /// <summary>Guinean franc</summary>
    GNF,

    /// <summary>Guatemalan quetzal</summary>
    GTQ,

    /// <summary>Guyanese dollar</summary>
    GYD,

    /// <summary>Hong Kong dollar</summary>
    HKD,

    /// <summary>Honduran lempira</summary>
    HNL,

    /// <summary>Haitian gourde</summary>
    HTG,

    /// <summary>Hungarian forint</summary>
    HUF,

    /// <summary>Indonesian rupiah</summary>
    IDR,

    /// <summary>Israeli new shekel</summary>
    ILS,

    /// <summary>Indian rupee</summary>
    INR,

    /// <summary>Iraqi dinar</summary>
    IQD,

    /// <summary>Iranian rial</summary>
    IRR,

    /// <summary>Icelandic krona</summary>
    ISK,

    /// <summary>Jamaican dollar</summary>
    JMD,

    /// <summary>Jordanian dinar</summary>
    JOD,

    /// <summary>Japanese yen</summary>
    JPY,

    /// <summary>Kenyan shilling</summary>
    KES,

    /// <summary>Kyrgyzstani som</summary>
    KGS,

    /// <summary>Cambodian riel</summary>
    KHR,

    /// <summary>Comoro franc</summary>
    KMF,

    /// <summary>North Korean won</summary>
    KPW,

    /// <summary>South Korean won</summary>
    KRW,

    /// <summary>Kuwaiti dinar</summary>
    KWD,

    /// <summary>Cayman Islands dollar</summary>
    KYD,

    /// <summary>Kazakhstani tenge</summary>
    KZT,

    /// <summary>Lao kip</summary>
    LAK,

    /// <summary>Lebanese pound</summary>
    LBP,

    /// <summary>Sri Lankan rupee</summary>
    LKR,

    /// <summary>Liberian dollar</summary>
    LRD,

    /// <summary>Lesotho loti</summary>
    LSL,

    /// <summary>Libyan dinar</summary>
    LYD,

    /// <summary>Moroccan dirham</summary>
    MAD,

    /// <summary>Moldovan leu</summary>
    MDL,

    /// <summary>Malagasy ariary</summary>
    MGA,

    /// <summary>Macedonian denar</summary>
    MKD,

    /// <summary>Myanmar kyat</summary>
    MMK,

    /// <summary>Mongolian togrog</summary>
    MNT,

    /// <summary>Macanese pataca</summary>
    MOP,

    /// <summary>Mauritanian ouguiya</summary>
    MRU,

    /// <summary>Mauritian rupee</summary>
    MUR,

    /// <summary>Maldivian rufiyaa</summary>
    MVR,

    /// <summary>Malawian kwacha</summary>
    MWK,

    /// <summary>Mexican peso</summary>
    MXN,

    /// <summary>Malaysian ringgit</summary>
    MYR,

    /// <summary>Mozambican metical</summary>
    MZN,

    /// <summary>Namibian dollar</summary>
    NAD,

    /// <summary>Nigerian naira</summary>
    NGN,

    /// <summary>Nicaraguan cordoba</summary>
    NIO,

    /// <summary>Norwegian krone</summary>
    NOK,

    /// <summary>Nepalese rupee</summary>
    NPR,

    /// <summary>New Zealand dollar</summary>
    NZD,

    /// <summary>Omani rial</summary>
    OMR,

    /// <summary>Panamanian balboa</summary>
    PAB,

    /// <summary>Peruvian sol</summary>
    PEN,

    /// <summary>Papua New Guinean kina</summary>
    PGK,

    /// <summary>Philippine peso</summary>
    PHP,

    /// <summary>Pakistani rupee</summary>
    PKR,

    /// <summary>Polish zloty</summary>
    PLN,

    /// <summary>Paraguayan guarani</summary>
    PYG,

    /// <summary>Qatari riyal</summary>
    QAR,

    /// <summary>Romanian leu</summary>
    RON,

    /// <summary>Serbian dinar</summary>
    RSD,

    /// <summary>Russian ruble</summary>
    RUB,

    /// <summary>Rwandan franc</summary>
    RWF,

    /// <summary>Saudi riyal</summary>
    SAR,

    /// <summary>Solomon Islands dollar</summary>
    SBD,

    /// <summary>Seychelles rupee</summary>
    SCR,

    /// <summary>Sudanese pound</summary>
    SDG,

    /// <summary>Swedish krona</summary>
    SEK,

    /// <summary>Singapore dollar</summary>
    SGD,

    /// <summary>Saint Helena pound</summary>
    SHP,

    /// <summary>Sierra Leonean leone</summary>
    SLE,

    /// <summary>Somali shilling</summary>
    SOS,

    /// <summary>Surinamese dollar</summary>
    SRD,

    /// <summary>South Sudanese pound</summary>
    SSP,

    /// <summary>Sao Tome and Principe dobra</summary>
    STN,

    /// <summary>Salvadoran colon</summary>
    SVC,

    /// <summary>Syrian pound</summary>
    SYP,

    /// <summary>Swazi lilangeni</summary>
    SZL,

    /// <summary>Thai baht</summary>
    THB,

    /// <summary>Tajikistani somoni</summary>
    TJS,

    /// <summary>Turkmenistan manat</summary>
    TMT,

    /// <summary>Tunisian dinar</summary>
    TND,

    /// <summary>Tongan pa'anga</summary>
    TOP,

    /// <summary>Turkish lira</summary>
    TRY,

    /// <summary>Trinidad and Tobago dollar</summary>
    TTD,

    /// <summary>New Taiwan dollar</summary>
    TWD,

    /// <summary>Tanzanian shilling</summary>
    TZS,

    /// <summary>Ukrainian hryvnia</summary>
    UAH,

    /// <summary>Ugandan shilling</summary>
    UGX,

    /// <summary>United States dollar</summary>
    USD,

    /// <summary>Uruguayan peso</summary>
    UYU,

    /// <summary>Uzbekistani sum</summary>
    UZS,

    /// <summary>Venezuelan digital bolivar</summary>
    VED,

    /// <summary>Venezuelan sovereign bolivar</summary>
    VES,

    /// <summary>Vietnamese dong</summary>
    VND,

    /// <summary>Vanuatu vatu</summary>
    VUV,

    /// <summary>Samoan tala</summary>
    WST,

    /// <summary>CFA franc BEAC</summary>
    XAF,

    /// <summary>East Caribbean dollar</summary>
    XCD,

    /// <summary>CFA franc BCEAO</summary>
    XOF,

    /// <summary>CFP franc</summary>
    XPF,

    /// <summary>Yemeni rial</summary>
    YER,

    /// <summary>South African rand</summary>
    ZAR,

    /// <summary>Zambian kwacha</summary>
    ZMW,

    /// <summary>Zimbabwe Gold</summary>
    ZWG
}
