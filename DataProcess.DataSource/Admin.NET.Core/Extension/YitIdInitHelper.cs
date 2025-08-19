// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

//namespace Admin.NET.Core;

///// <summary>
///// 使用Redis自动注册雪花Id的 WorkerId
///// </summary>
//public class YitIdInitHelper
//{
//    // 定义dll路径
//    public const string RegWorkerId_DLL_NAME = "lib\\regworkerid_lib_v1.3.1\\yitidgengo.dll";

//    // 注册一个 WorkerId，会先注销所有本机已注册的记录
//    // ip: redis 服务器地址
//    // port: redis 端口
//    // password: redis 访问密码，可为空字符串“”
//    // maxWorkerId: 最大 WorkerId
//    [DllImport(RegWorkerId_DLL_NAME, EntryPoint = "RegisterOne", CallingConvention = CallingConvention.Cdecl, ExactSpelling = false)]
//    private static extern ushort RegisterOne(string ip, int port, string password, int maxWorkerId);

//    // 注销本机已注册的 WorkerId
//    [DllImport(RegWorkerId_DLL_NAME, EntryPoint = "UnRegister", CallingConvention = CallingConvention.Cdecl, ExactSpelling = false)]
//    private static extern void UnRegister();

//    // 检查本地WorkerId是否有效（0-有效，其它-无效）
//    [DllImport(RegWorkerId_DLL_NAME, EntryPoint = "Validate", CallingConvention = CallingConvention.Cdecl, ExactSpelling = false)]
//    private static extern int Validate(int workerId);

//    public static long NextId()
//    {
//        // 此判断在高并发的情况下可能会有问题
//        if (YitIdHelper.IdGenInstance == null)
//        {
//            UnRegister();

//            // 如果不用自动注册WorkerId的话，直接传一个数值就可以了
//            var workerId = RegisterOne("127.0.0.1", 6379, "", 63);
//            // 创建 IdGeneratorOptions 对象，可在构造函数中输入 WorkerId：
//            var options = new IdGeneratorOptions(workerId);
//            // options.WorkerIdBitLength = 10; // 默认值6，限定 WorkerId 最大值为2^6-1，即默认最多支持64个节点。
//            // options.SeqBitLength = 6; // 默认值6，限制每毫秒生成的ID个数。若生成速度超过5万个/秒，建议加大 SeqBitLength 到 10。
//            // options.BaseTime = Your_Base_Time; // 如果要兼容老系统的雪花算法，此处应设置为老系统的BaseTime。
//            // ...... 其它参数参考 IdGeneratorOptions 定义。

//            // 保存参数（务必调用，否则参数设置不生效）：
//            YitIdHelper.SetIdGenerator(options);

//            // 以上过程只需全局一次，且应在生成ID之前完成。
//        }
//        return YitIdHelper.NextId();
//    }
//}