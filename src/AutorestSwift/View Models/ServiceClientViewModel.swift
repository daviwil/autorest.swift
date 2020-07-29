// --------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// The MIT License (MIT)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the ""Software""), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//
// --------------------------------------------------------------------------

import Foundation

private func getValue(from schema: Parameter, signatureParameters params: [ParameterViewModel]) -> String {
    if let constantSchema = schema.schema as? ConstantSchema {
        return "\"\(constantSchema.value.value)\""
    }

    // check if the name matches with one of the signature parameter
    // if match, the value is taken from the signature parameter
    for param in params where param.name == schema.name {
        return param.name
    }

    return ""
}

/// View Model for the service client file.
struct ServiceClientFileViewModel {
    let name: String
    let comment: ViewModelComment
    let operationGroups: [OperationGroupViewModel]

    init(from schema: CodeModel) {
        self.name = clientName(for: schema.name)
        self.comment = ViewModelComment(from: schema.description)
        var items = [OperationGroupViewModel]()
        for group in schema.operationGroups {
            items.append(OperationGroupViewModel(from: group))
        }
        self.operationGroups = items
    }
}

private func clientName(for serviceName: String) -> String {
    var name = serviceName
    let stripList = ["Service", "Client"]
    for item in stripList {
        if name.hasSuffix(item) {
            name = String(name.dropLast(item.count))
        }
    }
    return "\(name)Client"
}

private func operationName(for operationName: String) -> String {
    let pluralReplacements = [
        "get": "list"
    ]

    var nameComps = operationName.splitAndJoinAcronyms

    // TODO: Need a more reliable way to know whether the last item is singular or plural
    let isPlural = nameComps.last?.hasSuffix("s") ?? false
    for (index, comp) in nameComps.enumerated() {
        if index == 0 {
            if isPlural, let replacement = pluralReplacements[comp] {
                nameComps[index] = replacement
            }
        } else {
            nameComps[index] = comp.capitalized
        }
    }
    return nameComps.joined()
}
