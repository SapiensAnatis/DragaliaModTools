using System.Collections.Frozen;

namespace ModTools.Commands.Manifest;

internal sealed partial class AddEventAssetsCommand
{
    private record struct ManifestStartDate(DateOnly Date, string FolderName);

    private record struct EventPeriod(DateOnly StartDate, DateOnly EndDate);

    private static readonly IReadOnlyList<ManifestStartDate> ManifestDates =
    [
        new(new DateOnly(2019, 07, 10), "20190710_gvd7LUW"),
        new(new DateOnly(2019, 07, 15), "20190715_jcrFxbb"),
        new(new DateOnly(2019, 07, 19), "20190719_LAXNGJi"),
        new(new DateOnly(2019, 07, 22), "20190722_hdyIhZf"),
        new(new DateOnly(2019, 07, 23), "20190723_Gooofpg"),
        new(new DateOnly(2019, 07, 26), "20190726_ClUvjHe"),
        new(new DateOnly(2019, 07, 29), "20190729_h5ouYt2"),
        new(new DateOnly(2019, 07, 30), "20190730_SHfuOfK"),
        new(new DateOnly(2019, 07, 31), "20190731_GpZdycG"),
        new(new DateOnly(2019, 08, 09), "20190809_bdO1D4i"),
        new(new DateOnly(2019, 08, 13), "20190813_QKAJGOf"),
        new(new DateOnly(2019, 08, 16), "20190816_AraunrV"),
        new(new DateOnly(2019, 08, 19), "20190819_gUHqUK69lwbrAN7K"),
        new(new DateOnly(2019, 08, 22), "20190822_2LYBPLE7Zzyv5hwl"),
        new(new DateOnly(2019, 08, 26), "20190826_giWln02Zsvy9qOI5"),
        new(new DateOnly(2019, 08, 26), "20190826_ZeCVCgPZ7eN29aZK"),
        new(new DateOnly(2019, 08, 28), "20190828_nf35HFYSQgacSxTU"),
        new(new DateOnly(2019, 08, 31), "20190831_z8AC4jj7FJjYafx8"),
        new(new DateOnly(2019, 09, 09), "20190909_1vi5iWJRkmxRWm9i"),
        new(new DateOnly(2019, 09, 12), "20190912_wo54L6ay92jA3Sbm"),
        new(new DateOnly(2019, 09, 19), "20190919_fnQG5GuOnFWxm50l"),
        new(new DateOnly(2019, 09, 25), "20190925_mxDUfk82JA08fUwJ"),
        new(new DateOnly(2019, 09, 27), "20190927_2x2SmldasEvdxvnw"),
        new(new DateOnly(2019, 09, 30), "20190930_gqN09KVh8Dfpb7xc"),
        new(new DateOnly(2019, 10, 07), "20191007_WiFqSCahQKm9Z9LV"),
        new(new DateOnly(2019, 10, 11), "20191011_wW0WkBG35UkTpby9"),
        new(new DateOnly(2019, 10, 14), "20191014_jDwX2Lm4U8sWdoAO"),
        new(new DateOnly(2019, 10, 18), "20191018_tDJHXMpZY2uVjhae"),
        new(new DateOnly(2019, 10, 21), "20191021_jAcsPzwg9u6Vwvzf"),
        new(new DateOnly(2019, 10, 25), "20191025_sONQdJnd8k3nxNgS"),
        new(new DateOnly(2019, 10, 28), "20191028_1YYnlPvmbsFmXXKh"),
        new(new DateOnly(2019, 10, 28), "20191028_Bj2tc7LSNXSEVuDi"),
        new(new DateOnly(2019, 10, 28), "20191028_CNgEXRrnHesbn95V"),
        new(new DateOnly(2019, 10, 31), "20191031_V2TdY31HKGuVGtZX"),
        new(new DateOnly(2019, 11, 04), "20191104_IO3pigcuvvY370Sb"),
        new(new DateOnly(2019, 11, 11), "20191111_Dza2jgzTyGD2kqtt"),
        new(new DateOnly(2019, 11, 13), "20191113_9VhFm1zvp1K4pH4B"),
        new(new DateOnly(2019, 11, 15), "20191115_wEzW0k93SJYo6Hoj"),
        new(new DateOnly(2019, 11, 21), "20191121_Or7dmbfDyfBEwIay"),
        new(new DateOnly(2019, 11, 25), "20191125_nlXgzTCDKTYgcXMx"),
        new(new DateOnly(2019, 11, 28), "20191128_Cv08YN2j1pAV9MEa"),
        new(new DateOnly(2019, 11, 29), "20191129_vsvVWVaVNUUFp5QT"),
        new(new DateOnly(2019, 11, 29), "20191129_xuJH4C5xojHbMtHO"),
        new(new DateOnly(2019, 12, 02), "20191202_r6lXGauUPPsFteQb"),
        new(new DateOnly(2019, 12, 06), "20191206_g5Os0CXPvp66bLn3"),
        new(new DateOnly(2019, 12, 09), "20191209_3Hmv04ykAYNGOIEf"),
        new(new DateOnly(2019, 12, 11), "20191211_vj9zpOHXFzTAatDk"),
        new(new DateOnly(2019, 12, 24), "20191224_Pbgz4Yv8MQHAJU88"),
        new(new DateOnly(2019, 12, 30), "20191230_AnWMWFMWJSZbW2gh"),
        new(new DateOnly(2020, 01, 09), "20200109_lKqN94ajQOQy3Ijm"),
        new(new DateOnly(2020, 01, 13), "20200113_JrVIDT29D5qfB5f8"),
        new(new DateOnly(2020, 01, 20), "20200120_ESsP4CBK9nRt4uPe"),
        new(new DateOnly(2020, 01, 22), "20200122_1qMjTHHI5cgdsPhZ"),
        new(new DateOnly(2020, 01, 26), "20200126_N2nEciA1COcikO1A"),
        new(new DateOnly(2020, 01, 28), "20200128_txitq95v9zcvpOU3"),
        new(new DateOnly(2020, 02, 03), "20200203_mo0SpfeLkm01sr2e"),
        new(new DateOnly(2020, 02, 12), "20200212_lJf2gEp3N3NvCwIA"),
        new(new DateOnly(2020, 02, 13), "20200213_hpUgQJzDicxDVBOo"),
        new(new DateOnly(2020, 02, 19), "20200219_8kyRr6vyxmwBKvem"),
        new(new DateOnly(2020, 02, 26), "20200226_lg4RUkk5ZmNJVYIC"),
        new(new DateOnly(2020, 02, 27), "20200227_JSmpWnadD02uOWQA"),
        new(new DateOnly(2020, 03, 02), "20200302_ECYTKiuMMtfvbwx5"),
        new(new DateOnly(2020, 03, 11), "20200311_cdVHYYaI4I3f1Eow"),
        new(new DateOnly(2020, 03, 11), "20200311_SHnL6N0ZkmTZ7Wgm"),
        new(new DateOnly(2020, 03, 16), "20200316_n9WuhDYTjDY1EL2o"),
        new(new DateOnly(2020, 03, 22), "20200322_GZqHujfTw0tELlBH"),
        new(new DateOnly(2020, 03, 26), "20200326_DLbfu7p9sAbWMFUO"),
        new(new DateOnly(2020, 03, 30), "20200330_luV13xnogxB7nCDc"),
        new(new DateOnly(2020, 03, 31), "20200331_shEag34JF5RzdngP"),
        new(new DateOnly(2020, 04, 01), "20200401_jkC7PxXEehFyiH5S"),
        new(new DateOnly(2020, 04, 05), "20200405_PzafdlUrgKIudoSR"),
        new(new DateOnly(2020, 04, 09), "20200409_He3Nr1WR1xi0UA1R"),
        new(new DateOnly(2020, 04, 12), "20200412_UznxJTVdfsCF5dfN"),
        new(new DateOnly(2020, 04, 16), "20200416_FOPDCphVRS6E2kIz"),
        new(new DateOnly(2020, 04, 19), "20200419_xTkuL1AIhu64ZKTf"),
        new(new DateOnly(2020, 04, 24), "20200424_Ex9MN2NUi3dfQMN8"),
        new(new DateOnly(2020, 04, 27), "20200427_wy9sbf6b09GjGj94"),
        new(new DateOnly(2020, 04, 28), "20200428_ZGvPeVdOoREUQ4qA"),
        new(new DateOnly(2020, 04, 30), "20200430_40J8hmAQLSiCUo8O"),
        new(new DateOnly(2020, 04, 30), "20200430_5Jom2O5wqIzyEjyb"),
        new(new DateOnly(2020, 05, 04), "20200504_TWV4mAuWpWI3HMFw"),
        new(new DateOnly(2020, 05, 12), "20200512_tKHNb0acUXEZmrOs"),
        new(new DateOnly(2020, 05, 22), "20200522_rwszwc4sujligsus"),
        new(new DateOnly(2020, 05, 25), "20200525_EzVnD7X0RKecgzHj"),
        new(new DateOnly(2020, 05, 26), "20200526_DcTLUAV1Ocf0QXrO"),
        new(new DateOnly(2020, 05, 29), "20200529_iN8BUd6WE7mxdD3S"),
        new(new DateOnly(2020, 06, 03), "20200603_mpJUw7pLCaNUiGrS"),
        new(new DateOnly(2020, 06, 12), "20200612_zqLU44Xfbs4ZlaD3"),
        new(new DateOnly(2020, 06, 19), "20200619_nyLViPCy6ZhzhkqC"),
        new(new DateOnly(2020, 06, 22), "20200622_9K8R0HRaB72UPScR"),
        new(new DateOnly(2020, 06, 24), "20200624_U3Do0l3gdUdGIF4O"),
        new(new DateOnly(2020, 06, 29), "20200629_WLFrDSRo1ERAim5h"),
        new(new DateOnly(2020, 07, 03), "20200703_CpaDRUu8s7bpttdP"),
        new(new DateOnly(2020, 07, 10), "20200710_xcrdMepbPe7FpewP"),
        new(new DateOnly(2020, 07, 13), "20200713_Rkxo5aqoFhIwJcDZ"),
        new(new DateOnly(2020, 07, 15), "20200715_PKHBHM88AnNBysii"),
        new(new DateOnly(2020, 07, 20), "20200720_f0gNq2SiiPji7U7M"),
        new(new DateOnly(2020, 07, 22), "20200722_PKSMSEATXf115eI0"),
        new(new DateOnly(2020, 07, 27), "20200727_iEbaxWlPzam57rtN"),
        new(new DateOnly(2020, 07, 28), "20200728_wAJMIhFmKiQqpYvG"),
        new(new DateOnly(2020, 07, 30), "20200730_Dqlp7R50gYHxh4X5"),
        new(new DateOnly(2020, 08, 07), "20200807_V2S6OgtlaWweabI0"),
        new(new DateOnly(2020, 08, 11), "20200811_pEuTr3ESvvu79rDw"),
        new(new DateOnly(2020, 08, 14), "20200814_h7UQrBKRO57EkQZZ"),
        new(new DateOnly(2020, 08, 18), "20200818_vtKaxXkEk5bsins4"),
        new(new DateOnly(2020, 08, 25), "20200825_n5A2dj4S36C2evlp"),
        new(new DateOnly(2020, 08, 27), "20200827_ZyCWBkDyfYZ0h3jU"),
        new(new DateOnly(2020, 08, 28), "20200828_GOKEKuAWG3oeQCWy"),
        new(new DateOnly(2020, 09, 03), "20200903_7uHxFwU0ErUDR4yY"),
        new(new DateOnly(2020, 09, 09), "20200909_tlrNhTagkBMXZyKM"),
        new(new DateOnly(2020, 09, 11), "20200911_3fck5gxkH73tbUxo"),
        new(new DateOnly(2020, 09, 16), "20200916_PERQFzDnXPqsgjGh"),
        new(new DateOnly(2020, 09, 21), "20200921_IeLazHAje7oFnpbV"),
        new(new DateOnly(2020, 09, 25), "20200925_plkaDGmSrHL2hhe8"),
        new(new DateOnly(2020, 09, 27), "20200927_MV2M0tB8d23y0f3M"),
        new(new DateOnly(2020, 09, 28), "20200928_adGcr2GTaGec47Fw"),
        new(new DateOnly(2020, 09, 30), "20200930_vOdu87tvIj7oEBu9"),
        new(new DateOnly(2020, 10, 05), "20201005_ctA0ok1hpqbQLNpi"),
        new(new DateOnly(2020, 10, 09), "20201009_rUAJ84rrQoQKbHPT"),
        new(new DateOnly(2020, 10, 12), "20201012_lgAMJZOe6RAUaGfu"),
        new(new DateOnly(2020, 10, 19), "20201019_i6KrmfdfCODkjIoY"),
        new(new DateOnly(2020, 10, 21), "20201021_0s9kIoOlyfJCynPl"),
        new(new DateOnly(2020, 10, 22), "20201022_uEFtwvE4V2GcTlHf"),
        new(new DateOnly(2020, 10, 27), "20201027_zJ5tYnaD7CKBdsiV"),
        new(new DateOnly(2020, 10, 30), "20201030_3onIYAGX7C8sVusx"),
        new(new DateOnly(2020, 11, 02), "20201102_i2KQUEjJSBn8arFS"),
        new(new DateOnly(2020, 11, 04), "20201104_bLJAeZLv7OyXr5vC"),
        new(new DateOnly(2020, 11, 11), "20201111_2WwgQ0wZyx4oYt7S"),
        new(new DateOnly(2020, 11, 11), "20201111_Q9dwsklV74JRzQk3"),
        new(new DateOnly(2020, 11, 13), "20201113_ziG2a3wZmqghCYnc"),
        new(new DateOnly(2020, 11, 17), "20201117_0RerC7y9c9oBQ2mK"),
        new(new DateOnly(2020, 11, 24), "20201124_08NV7KO9YyXMIlB2"),
        new(new DateOnly(2020, 11, 27), "20201127_3LpNo8zQRmEBWpKY"),
        new(new DateOnly(2020, 11, 27), "20201127_aBYpbLFVwmiOGOrC"),
        new(new DateOnly(2020, 11, 30), "20201130_5PTeAdEsB0WYRzxy"),
        new(new DateOnly(2020, 12, 07), "20201207_gh2V8gX93j1K5xyD"),
        new(new DateOnly(2020, 12, 14), "20201214_w3z3UrxBrkDE8Jsq"),
        new(new DateOnly(2020, 12, 15), "20201215_PQ4QGdAoBUsWzYC1"),
        new(new DateOnly(2020, 12, 18), "20201218_zzMasNuJvEhRLhoR"),
        new(new DateOnly(2020, 12, 28), "20201228_zD3NCu1eqObsWSK4"),
        new(new DateOnly(2020, 12, 31), "20201231_SAcWiaRBfYDT4FGS"),
        new(new DateOnly(2021, 01, 07), "20210107_wp3GuzxBjCGZ6woB"),
        new(new DateOnly(2021, 01, 13), "20210113_YDqw9udArvIfUZfh"),
        new(new DateOnly(2021, 01, 14), "20210114_vowpPQc9CWVLsh43"),
        new(new DateOnly(2021, 01, 18), "20210118_blAHaHz3JJzPrQ4t"),
        new(new DateOnly(2021, 01, 26), "20210126_vtCkYNskH0VtZQ17"),
        new(new DateOnly(2021, 01, 27), "20210127_ceixwiGqBzfECogS"),
        new(new DateOnly(2021, 01, 31), "20210131_lqfRBhj2APlqf8RC"),
        new(new DateOnly(2021, 02, 06), "20210206_qhiM5GTUdoEyhyZO"),
        new(new DateOnly(2021, 02, 12), "20210212_w26WpMYYeMupjvMi"),
        new(new DateOnly(2021, 02, 16), "20210216_isXBbeSHYyjFi8x8"),
        new(new DateOnly(2021, 02, 18), "20210218_WRWUKhEUZ1zssD3x"),
        new(new DateOnly(2021, 02, 25), "20210225_TkP4l5wRyWrRUUhS"),
        new(new DateOnly(2021, 02, 26), "20210226_9gBlPzXeb1ZQ8nhE"),
        new(new DateOnly(2021, 03, 03), "20210303_VuzEbSvGXE9GePSr"),
        new(new DateOnly(2021, 03, 05), "20210305_3yMAGQJgvms5FYf8"),
        new(new DateOnly(2021, 03, 09), "20210309_nRiaUK6w2SegGSPi"),
        new(new DateOnly(2021, 03, 12), "20210312_GOPtsDC3E6QiF5Cv"),
        new(new DateOnly(2021, 03, 16), "20210316_J1eYJzPMfE4XAj3L"),
        new(new DateOnly(2021, 03, 22), "20210322_KT53ClV4RXFvW9E7"),
        new(new DateOnly(2021, 03, 25), "20210325_r5a6coMxoGOvBd6t"),
        new(new DateOnly(2021, 03, 26), "20210326_Un0bkHWwy32qpXz3"),
        new(new DateOnly(2021, 03, 27), "20210327_4FVz205eYMAyS8lm"),
        new(new DateOnly(2021, 04, 01), "20210401_B8oDqcD4n595WW4S"),
        new(new DateOnly(2021, 04, 02), "20210402_EpsR0JB7zSO5w4Pn"),
        new(new DateOnly(2021, 04, 07), "20210407_vV26vt33rpPRoANd"),
        new(new DateOnly(2021, 04, 12), "20210412_725KSe0jjK7dUQZq"),
        new(new DateOnly(2021, 04, 15), "20210415_tKNhlutrX4LPXPUk"),
        new(new DateOnly(2021, 04, 19), "20210419_vX73NiY8Guq7cM1e"),
        new(new DateOnly(2021, 04, 22), "20210422_aEDjUc4wdXrv19fT"),
        new(new DateOnly(2021, 04, 27), "20210427_Ot0GTYt1zLwsaOXB"),
        new(new DateOnly(2021, 04, 28), "20210428_w6keqzyZBMUIoRDS"),
        new(new DateOnly(2021, 04, 30), "20210430_c4d5ByZ4xpbjAO3e"),
        new(new DateOnly(2021, 05, 03), "20210503_Kq72Un8WifoDBM5F"),
        new(new DateOnly(2021, 05, 10), "20210510_dwbvNrRemWPrRrM5"),
        new(new DateOnly(2021, 05, 13), "20210513_ifOfPf3yu0McfeK7"),
        new(new DateOnly(2021, 05, 17), "20210517_y7mMOZkLLfhLRi0w"),
        new(new DateOnly(2021, 05, 19), "20210519_OZuxGHxRaHfdO6li"),
        new(new DateOnly(2021, 05, 25), "20210525_SDpfeV5FQQnDwPP9"),
        new(new DateOnly(2021, 05, 27), "20210527_kB2MLWQWg56aHAXB"),
        new(new DateOnly(2021, 05, 28), "20210528_RzuJnd8Cn0vF0AAU"),
        new(new DateOnly(2021, 06, 03), "20210603_QcGdhH0wnumSRjeY"),
        new(new DateOnly(2021, 06, 09), "20210609_EYj5KEnXEyfP3K6A"),
        new(new DateOnly(2021, 06, 15), "20210615_Z7MLBsh131Oql9x5"),
        new(new DateOnly(2021, 06, 21), "20210621_Xg4ouqgaEO1OZF6o"),
        new(new DateOnly(2021, 06, 25), "20210625_QftKwdOkiwKSuvNr"),
        new(new DateOnly(2021, 06, 29), "20210629_IhdhISbOqz7gNGAg"),
        new(new DateOnly(2021, 06, 30), "20210630_aYfogz4YpMIoIGqm"),
        new(new DateOnly(2021, 07, 02), "20210702_YNwOzst3Uo3AZtGQ"),
        new(new DateOnly(2021, 07, 03), "20210703_YNwOzst3Uo3AZtGQ"),
        new(new DateOnly(2021, 07, 05), "20210705_fI7jhauwoSwa6wYg"),
        new(new DateOnly(2021, 07, 12), "20210712_8KoUb8Dh71LI4SZS"),
        new(new DateOnly(2021, 07, 15), "20210715_9T78nbAeJt6R7UP4"),
        new(new DateOnly(2021, 07, 21), "20210721_OErSXNe01phID4cJ"),
        new(new DateOnly(2021, 07, 26), "20210726_67oUWzJD2q6UwKL0"),
        new(new DateOnly(2021, 07, 29), "20210729_ygkuUo8HAhsGHfQQ"),
        new(new DateOnly(2021, 07, 30), "20210730_GIiUkRuxTFVADnPN"),
        new(new DateOnly(2021, 07, 31), "20210731_GIiUkRuxTFVADnPN"),
        new(new DateOnly(2021, 08, 04), "20210804_dmUn1AOUkwenN2ye"),
        new(new DateOnly(2021, 08, 11), "20210811_SnCsWZNqVRTD4HvW"),
        new(new DateOnly(2021, 08, 13), "20210813_EPivsIMAdWxHQEWS"),
        new(new DateOnly(2021, 08, 17), "20210817_jy9FWdnzpzNfIrGm"),
        new(new DateOnly(2021, 08, 21), "20210821_4OZu75DkU2C28E46"),
        new(new DateOnly(2021, 08, 28), "20210828_S8IkeV14kdrMEPRP"),
        new(new DateOnly(2021, 08, 30), "20210830_eaWO0TQ0xXWXOPoe"),
        new(new DateOnly(2021, 09, 03), "20210903_RXvvCjrgJTRPPOcj"),
        new(new DateOnly(2021, 09, 13), "20210913_a0etJGKf8G7Y1gKy"),
        new(new DateOnly(2021, 09, 22), "20210922_NceYC42v2FDyUQ8P"),
        new(new DateOnly(2021, 09, 25), "20210925_Jx5NWnuqQQ27WCj9"),
        new(new DateOnly(2021, 09, 27), "20210927_PGDIDwGj4Fp665gH"),
        new(new DateOnly(2021, 09, 30), "20210930_e9huiUmZQ78mbKMB"),
        new(new DateOnly(2021, 10, 05), "20211005_4HBqqJ5bvXgdNLiV"),
        new(new DateOnly(2021, 10, 11), "20211011_4HBqqJ5bvXgdNLiV"),
        new(new DateOnly(2021, 10, 13), "20211013_ccWWbFWDwvSUdKlw"),
        new(new DateOnly(2021, 10, 15), "20211015_zajwQY4CyhPSR9W6"),
        new(new DateOnly(2021, 10, 28), "20211028_P5vciqNVlQONmeQr"),
        new(new DateOnly(2021, 10, 30), "20211030_ogF2rPANOIiF1kKI"),
        new(new DateOnly(2021, 11, 12), "20211112_xCjuv9SQUTIy4TzS"),
        new(new DateOnly(2021, 11, 16), "20211116_NoZYvi2xmNPPPPte"),
        new(new DateOnly(2021, 11, 25), "20211125_4pIw5kVtzMGtStds"),
        new(new DateOnly(2021, 11, 29), "20211129_h6lObp9eiVabAdyO"),
        new(new DateOnly(2021, 12, 03), "20211203_FBwAeqnZB4rwEVb6"),
        new(new DateOnly(2021, 12, 09), "20211209_OCz4WuqjAOAR6Shu"),
        new(new DateOnly(2021, 12, 15), "20211215_7wptbjHCMn1AFXdT"),
        new(new DateOnly(2021, 12, 24), "20211224_lxdHqe6iOoxFVjFK"),
        new(new DateOnly(2021, 12, 24), "20211224_WbqJTSlcL6noXYEm"),
        new(new DateOnly(2021, 12, 27), "20211227_XGHmpAfZ7EhrqU09"),
        new(new DateOnly(2021, 12, 31), "20211231_NHan3Y7Fnkeja8Ss"),
        new(new DateOnly(2022, 01, 05), "20220105_ADAlv8Tvv8y2Mhyr"),
        new(new DateOnly(2022, 01, 19), "20220119_3Dz5Qju6JbuGKAvG"),
        new(new DateOnly(2022, 01, 25), "20220125_dFWk3YFKQaLyBjKr"),
        new(new DateOnly(2022, 01, 31), "20220131_OUgxvK81fQpaxFUz"),
        new(new DateOnly(2022, 02, 04), "20220204_2C0917SnsI7PI7qD"),
        new(new DateOnly(2022, 02, 09), "20220209_UWHJ8UNxZaJsQpZy"),
        new(new DateOnly(2022, 02, 14), "20220214_yLgDW73F4XfhyeOP"),
        new(new DateOnly(2022, 02, 27), "20220227_E3SSsji2DskZWfBE"),
        new(new DateOnly(2022, 02, 27), "20220227_xWkMe8iEYpgigPEp"),
        new(new DateOnly(2022, 02, 28), "20220228_alqTjn0eSZxYe7JH"),
        new(new DateOnly(2022, 03, 05), "20220305_Jor4lgxf8ghwJaPk"),
        new(new DateOnly(2022, 03, 14), "20220314_sIs16NsuuCLCab45"),
        new(new DateOnly(2022, 03, 24), "20220324_1JItA01ciuApv4da"),
        new(new DateOnly(2022, 03, 27), "20220327_m2FxVe6wYt6pgwIO"),
        new(new DateOnly(2022, 03, 31), "20220331_xzjz7KnwLIMAv7Ng"),
        new(new DateOnly(2022, 04, 06), "20220406_hrXhxWDK5Lbx9dF8"),
        new(new DateOnly(2022, 04, 25), "20220425_bG2tI7opLU3lFWPs"),
        new(new DateOnly(2022, 05, 26), "20220526_6Oc258nSRXkA000P"),
        new(new DateOnly(2022, 07, 15), "20220715_6dWo56c90n7dFSBt"),
        new(new DateOnly(2022, 07, 25), "20220725_63aRsa6esQunh12v"),
        new(new DateOnly(2022, 10, 02), "20221002_y2XM6giU6zz56wCm"),
    ];

    /// <summary>
    /// Mapping of event ID to most recent run date. Used for determining which manifest contains an event's assets.
    /// </summary>
    /// <remarks>
    /// Generated with this SQLite query against the master asset database (plus some multi-line editing touch ups):
    /// <pre>
    /// <code>
    /// WITH non_compendium_events AS (SELECT EventData._Id as ID, MAX(EventData._StartDate) AS LatestRunStartDate, MAX(EventData._EndDate) AS LatestRunEndDate, TextLabel._Text AS EventName
    ///     FROM EventData
    ///     JOIN TextLabel on EventData._Name = TextLabel._Id
    ///     WHERE NOT EXISTS (SELECT 1
    /// FROM EventData e2
    ///     JOIN TextLabel t2 on e2._Name = t2._Id
    ///     WHERE t2._Text = TextLabel._Text
    ///     AND e2._IsMemoryEvent = 1)
    /// AND EventData._EventKindType <> 11 -- Battle royale
    /// GROUP BY TextLabel._Text)
    /// SELECT '[' || ID || '] = new(' || LatestRunStartDate || ', ' || LatestRunEndDate || ') // ' || EventName
    ///     FROM non_compendium_events
    ///
    /// </code>
    /// </pre>
    /// </remarks>
    private static readonly FrozenDictionary<int, EventPeriod> LatestEventRunStartDates =
        new Dictionary<int, EventPeriod>()
        {
            [20451] = new(new DateOnly(2022, 06, 30), new DateOnly(2022, 07, 11)), // A Clawful Caper
            [20453] = new(new DateOnly(2022, 08, 01), new DateOnly(2022, 08, 10)), // A Splash of Adventure
            [31007] = new(new DateOnly(2021, 12, 24), new DateOnly(2021, 12, 31)), // A Sweeping Retrospective
            [20446] = new(new DateOnly(2022, 03, 22), new DateOnly(2022, 03, 30)), // A Waltz with Fate
            [22230] = new(new DateOnly(2022, 06, 10), new DateOnly(2022, 06, 20)), // Ageless Artifice
            [31006] = new(new DateOnly(2021, 07, 19), new DateOnly(2021, 07, 30)), // Ascent to Eminence
            [20429] = new(new DateOnly(2021, 01, 31), new DateOnly(2021, 02, 12)), // Caged Desire
            [20460] = new(new DateOnly(2022, 11, 10), new DateOnly(2022, 11, 21)), // Celestial Showdown
            [20442] = new(new DateOnly(2021, 12, 16), new DateOnly(2021, 12, 25)), // Cursed Connections
            [20457] = new(new DateOnly(2022, 09, 30), new DateOnly(2022, 10, 10)), // Dawn of Dragalia
            [20454] = new(new DateOnly(2022, 08, 19), new DateOnly(2022, 08, 30)), // Doomsday Getaway
            [22232] = new(new DateOnly(2022, 08, 10), new DateOnly(2022, 08, 19)), // Drifting Sorrows
            [20448] = new(new DateOnly(2022, 04, 29), new DateOnly(2022, 05, 10)), // Echoes of Antiquity
            [20456] = new(new DateOnly(2022, 09, 20), new DateOnly(2022, 09, 30)), // Fortune from Afar
            [22221] = new(new DateOnly(2022, 01, 13), new DateOnly(2022, 01, 21)), // Fortune's Fray
            [20459] = new(new DateOnly(2022, 10, 20), new DateOnly(2022, 10, 31)), // Kindness and Captivity
            [22907] = new(new DateOnly(2022, 10, 31), new DateOnly(2022, 11, 10)), // Knights of Alberia
            [20458] = new(new DateOnly(2022, 10, 10), new DateOnly(2022, 10, 20)), // Loyalty's Requiem
            [21801] = new(new DateOnly(2019, 11, 29), new DateOnly(2019, 12, 16)), // Mega Man: Chaos Protocol
            [22001] = new(new DateOnly(2020, 01, 29), new DateOnly(2020, 02, 17)), // Monster Hunter: Primal Crisis
            [22225] = new(new DateOnly(2022, 03, 10), new DateOnly(2022, 03, 22)), // Nadine and Linnea's United Front
            [22229] = new(new DateOnly(2022, 05, 10), new DateOnly(2022, 05, 20)), // Northern Negotiators
            [22903] = new(new DateOnly(2021, 11, 29), new DateOnly(2021, 12, 09)), // One Starry Dragonyule
            [22905] = new(new DateOnly(2022, 02, 28), new DateOnly(2022, 03, 10)), // Operation: At Your Service
            [22205] = new(new DateOnly(2020, 10, 12), new DateOnly(2020, 10, 19)), // Postmortem Panic
            [20445] = new(new DateOnly(2022, 01, 21), new DateOnly(2022, 01, 31)), // Princess Connect! Re: Dive: A Voracious Visitor
            [20461] = new(new DateOnly(2022, 11, 21), new DateOnly(2022, 11, 29)), // Rage of Chronos
            [20447] = new(new DateOnly(2022, 04, 22), new DateOnly(2022, 04, 29)), // Resplendent Refrain
            [22222] = new(new DateOnly(2022, 02, 14), new DateOnly(2022, 02, 28)), // Romance Under Siege
            [22228] = new(new DateOnly(2022, 04, 15), new DateOnly(2022, 04, 22)), // Sands of Revelation
            [20455] = new(new DateOnly(2022, 08, 30), new DateOnly(2022, 09, 09)), // Scars of the Syndicate
            [22233] = new(new DateOnly(2022, 09, 09), new DateOnly(2022, 09, 20)), // Shackles of the Syndicate
            [20450] = new(new DateOnly(2022, 05, 30), new DateOnly(2022, 06, 10)), // Skyborne Spectacle
            [21305] = new(new DateOnly(2020, 10, 19), new DateOnly(2020, 10, 27)), // Stirring Shadows
            [20452] = new(new DateOnly(2022, 07, 20), new DateOnly(2022, 08, 01)), // Stranded Scions
            [22904] = new(new DateOnly(2022, 01, 31), new DateOnly(2022, 02, 14)), // The Blood That Binds
            [22906] = new(new DateOnly(2022, 06, 20), new DateOnly(2022, 06, 30)), // The Children of Yggdrasil
            [22231] = new(new DateOnly(2022, 07, 11), new DateOnly(2022, 07, 20)), // The Fabled Fortune
            [22218] = new(new DateOnly(2021, 12, 09), new DateOnly(2021, 12, 17)), // The Great Dragonyule Offensive
            [20449] = new(new DateOnly(2022, 05, 20), new DateOnly(2022, 05, 30)), // Timeworn Torment
            [21304] = new(new DateOnly(2020, 02, 14), new DateOnly(2020, 02, 28)), // Valentine's Confections
            [31002] = new(new DateOnly(2020, 04, 01), new DateOnly(2020, 04, 02)), // Wagabond Pupper
        }.ToFrozenDictionary();
}
